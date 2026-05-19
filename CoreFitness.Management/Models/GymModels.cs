namespace CoreFitness.Management.Models;

public enum MemberStatus
{
    Active,
    ExpiringSoon,
    Expired,
    Blocked
}

public sealed class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PlanName { get; set; } = "Monthly";
    public DateOnly JoinedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly FeeExpiresOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
    public decimal MonthlyFee { get; set; }
    public string AssignedTrainer { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool ManualBlock { get; set; }

    public MemberStatus Status
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (ManualBlock)
            {
                return MemberStatus.Blocked;
            }

            if (FeeExpiresOn < today)
            {
                return MemberStatus.Expired;
            }

            if (FeeExpiresOn <= today.AddDays(7))
            {
                return MemberStatus.ExpiringSoon;
            }

            return MemberStatus.Active;
        }
    }

    public bool IsEntryAllowed => Status is MemberStatus.Active or MemberStatus.ExpiringSoon;
    public decimal DueAmount => FeeExpiresOn < DateOnly.FromDateTime(DateTime.Today) ? MonthlyFee : 0;
}

public sealed class Trainer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public decimal MonthlySalary { get; set; }
    public DateOnly JoinedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool Active { get; set; } = true;
}

public sealed class Locker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LockerNumber { get; set; } = string.Empty;
    public Guid? MemberId { get; set; }
    public DateOnly? AssignedUntil { get; set; }
    public decimal MonthlyRent { get; set; }
}

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Outlet { get; set; } = "Reception";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public sealed class Sale
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime SoldAt { get; set; } = DateTime.Now;
    public string SoldBy { get; set; } = "Front Desk";
    public string Outlet { get; set; } = "Reception";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Total => Quantity * UnitPrice;
}

public sealed record SaleResult(bool Success, string Message, Guid? SaleId);

public sealed class PromotionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateOnly StartsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndsOn { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
    public bool Active { get; set; } = true;
}

public sealed class DoorAccessEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MemberCode { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.Now;
    public bool Allowed { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class GymState
{
    public List<Member> Members { get; set; } = [];
    public List<Trainer> Trainers { get; set; } = [];
    public List<Locker> Lockers { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public List<Sale> Sales { get; set; } = [];
    public List<PromotionPlan> Promotions { get; set; } = [];
    public List<DoorAccessEvent> DoorAccessEvents { get; set; } = [];
}
