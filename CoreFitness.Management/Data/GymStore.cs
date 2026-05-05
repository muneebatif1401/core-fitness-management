using System.Text.Json;
using CoreFitness.Management.Models;

namespace CoreFitness.Management.Data;

public sealed class GymStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private GymState _state;

    public GymStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _path = Path.Combine(dataDirectory, "core-fitness.json");
        _state = Load();
    }

    public IReadOnlyList<Member> Members => Snapshot().Members.OrderBy(m => m.FeeExpiresOn).ToList();
    public IReadOnlyList<Trainer> Trainers => Snapshot().Trainers.OrderBy(t => t.FullName).ToList();
    public IReadOnlyList<Locker> Lockers => Snapshot().Lockers.OrderBy(l => l.LockerNumber).ToList();
    public IReadOnlyList<Product> Products => Snapshot().Products.OrderBy(p => p.Name).ToList();
    public IReadOnlyList<Sale> Sales => Snapshot().Sales.OrderByDescending(s => s.SoldAt).ToList();
    public IReadOnlyList<PromotionPlan> Promotions => Snapshot().Promotions.OrderByDescending(p => p.StartsOn).ToList();
    public IReadOnlyList<DoorAccessEvent> DoorAccessEvents => Snapshot().DoorAccessEvents.OrderByDescending(e => e.CheckedAt).ToList();

    public IReadOnlyList<Member> ExpiringMembers => Members.Where(m => m.Status == MemberStatus.ExpiringSoon).ToList();
    public IReadOnlyList<Member> DueMembers => Members.Where(m => m.Status is MemberStatus.Expired or MemberStatus.Blocked).ToList();
    public decimal TodaySalesTotal => Sales.Where(s => s.SoldAt.Date == DateTime.Today).Sum(s => s.Total);
    public decimal DueFeesTotal => DueMembers.Sum(m => m.DueAmount);

    public void AddMember(Member member)
    {
        lock (_gate)
        {
            member.MemberCode = string.IsNullOrWhiteSpace(member.MemberCode) ? NextMemberCode() : member.MemberCode.Trim().ToUpperInvariant();
            _state.Members.Add(member);
            Save();
        }
    }

    public void RenewMember(Guid memberId, int months)
    {
        lock (_gate)
        {
            var member = _state.Members.FirstOrDefault(m => m.Id == memberId);
            if (member is null)
            {
                return;
            }

            var start = member.FeeExpiresOn < DateOnly.FromDateTime(DateTime.Today)
                ? DateOnly.FromDateTime(DateTime.Today)
                : member.FeeExpiresOn;
            member.FeeExpiresOn = start.AddMonths(Math.Max(1, months));
            member.ManualBlock = false;
            Save();
        }
    }

    public void ToggleMemberBlock(Guid memberId)
    {
        lock (_gate)
        {
            var member = _state.Members.FirstOrDefault(m => m.Id == memberId);
            if (member is null)
            {
                return;
            }

            member.ManualBlock = !member.ManualBlock;
            Save();
        }
    }

    public void AddTrainer(Trainer trainer)
    {
        lock (_gate)
        {
            _state.Trainers.Add(trainer);
            Save();
        }
    }

    public void AssignLocker(Guid lockerId, Guid? memberId, DateOnly? assignedUntil)
    {
        lock (_gate)
        {
            var locker = _state.Lockers.FirstOrDefault(l => l.Id == lockerId);
            if (locker is null)
            {
                return;
            }

            locker.MemberId = memberId;
            locker.AssignedUntil = memberId.HasValue ? assignedUntil : null;
            Save();
        }
    }

    public string RecordSale(Guid productId, int quantity, string soldBy)
    {
        lock (_gate)
        {
            var product = _state.Products.FirstOrDefault(p => p.Id == productId);
            if (product is null)
            {
                return "Product not found.";
            }

            if (quantity < 1)
            {
                return "Quantity must be at least 1.";
            }

            if (product.Stock < quantity)
            {
                return "Not enough stock for this sale.";
            }

            product.Stock -= quantity;
            _state.Sales.Add(new Sale
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = quantity,
                UnitPrice = product.Price,
                SoldBy = string.IsNullOrWhiteSpace(soldBy) ? "Front Desk" : soldBy.Trim()
            });
            Save();
            return "Sale recorded.";
        }
    }

    public void AddPromotion(PromotionPlan promotion)
    {
        lock (_gate)
        {
            _state.Promotions.Add(promotion);
            Save();
        }
    }

    public DoorAccessEvent CheckDoorAccess(string memberCode)
    {
        lock (_gate)
        {
            var normalizedCode = (memberCode ?? string.Empty).Trim().ToUpperInvariant();
            var member = _state.Members.FirstOrDefault(m => m.MemberCode.Equals(normalizedCode, StringComparison.OrdinalIgnoreCase));
            var entry = new DoorAccessEvent { MemberCode = normalizedCode };

            if (member is null)
            {
                entry.Allowed = false;
                entry.Reason = "Member not found";
            }
            else
            {
                entry.MemberName = member.FullName;
                entry.Allowed = member.IsEntryAllowed;
                entry.Reason = member.IsEntryAllowed ? "Access granted" : $"Blocked: {member.Status}";
            }

            _state.DoorAccessEvents.Add(entry);
            Save();
            return entry;
        }
    }

    public string WhatsAppReminderUrl(Member member)
    {
        var phone = new string(member.Phone.Where(char.IsDigit).ToArray());
        var message = Uri.EscapeDataString($"Hello {member.FullName}, your CORE FITNESS fee expires on {member.FeeExpiresOn:dd MMM yyyy}. Please renew to keep door access active.");
        return string.IsNullOrWhiteSpace(phone) ? "#" : $"https://wa.me/{phone}?text={message}";
    }

    public string MemberName(Guid? memberId)
    {
        if (!memberId.HasValue)
        {
            return "Available";
        }

        return Members.FirstOrDefault(m => m.Id == memberId.Value)?.FullName ?? "Unknown member";
    }

    private GymState Snapshot()
    {
        lock (_gate)
        {
            return _state;
        }
    }

    private GymState Load()
    {
        if (!File.Exists(_path))
        {
            var seeded = Seed();
            File.WriteAllText(_path, JsonSerializer.Serialize(seeded, _jsonOptions));
            return seeded;
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<GymState>(json) ?? Seed();
    }

    private void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_state, _jsonOptions));

    private string NextMemberCode()
    {
        var nextNumber = _state.Members
            .Select(m => m.MemberCode.Replace("CF-", string.Empty))
            .Select(value => int.TryParse(value, out var number) ? number : 0)
            .DefaultIfEmpty(1000)
            .Max() + 1;

        return $"CF-{nextNumber}";
    }

    private static GymState Seed()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var members = new List<Member>
        {
            new()
            {
                MemberCode = "CF-1001",
                FullName = "Adeel Khan",
                Phone = "923001234567",
                PlanName = "Monthly Strength",
                FeeExpiresOn = today.AddDays(5),
                MonthlyFee = 6500,
                AssignedTrainer = "Sara Malik"
            },
            new()
            {
                MemberCode = "CF-1002",
                FullName = "Hira Ahmed",
                Phone = "923331234567",
                PlanName = "Quarterly Performance",
                FeeExpiresOn = today.AddMonths(2),
                MonthlyFee = 18000,
                AssignedTrainer = "Bilal Shah"
            },
            new()
            {
                MemberCode = "CF-1003",
                FullName = "Usman Raza",
                Phone = "923211234567",
                PlanName = "Monthly",
                FeeExpiresOn = today.AddDays(-2),
                MonthlyFee = 6000
            }
        };

        return new GymState
        {
            Members = members,
            Trainers =
            [
                new() { FullName = "Sara Malik", Phone = "923441112233", Specialty = "Strength Conditioning", MonthlySalary = 90000 },
                new() { FullName = "Bilal Shah", Phone = "923451112233", Specialty = "Functional Training", MonthlySalary = 85000 }
            ],
            Lockers =
            [
                new() { LockerNumber = "L-01", MemberId = members[0].Id, AssignedUntil = today.AddMonths(1), MonthlyRent = 1500 },
                new() { LockerNumber = "L-02", MonthlyRent = 1500 },
                new() { LockerNumber = "L-03", MonthlyRent = 1500 },
                new() { LockerNumber = "L-04", MonthlyRent = 1500 }
            ],
            Products =
            [
                new() { Name = "Whey Protein 2lb", Category = "Supplement", Price = 14500, Stock = 12 },
                new() { Name = "Pre Workout", Category = "Supplement", Price = 8500, Stock = 9 },
                new() { Name = "Shaker Bottle", Category = "Accessory", Price = 1800, Stock = 25 }
            ],
            Sales =
            [
                new() { ProductName = "Shaker Bottle", Quantity = 2, UnitPrice = 1800, SoldAt = DateTime.Now.AddHours(-2), SoldBy = "Front Desk" }
            ],
            Promotions =
            [
                new() { Name = "Summer Shred", Description = "15% off quarterly plans", DiscountPercent = 15, StartsOn = today, EndsOn = today.AddMonths(1) }
            ]
        };
    }
}
