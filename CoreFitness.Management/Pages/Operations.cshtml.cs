using CoreFitness.Management.Data;
using CoreFitness.Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

public sealed class OperationsModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;

    [BindProperty]
    public string MemberCode { get; set; } = string.Empty;

    [BindProperty]
    public Guid LockerId { get; set; }

    [BindProperty]
    public Guid? LockerMemberId { get; set; }

    [BindProperty]
    public DateOnly? AssignedUntil { get; set; }

    [BindProperty]
    public PromotionPlan PromotionInput { get; set; } = new();

    public DoorAccessEvent? DoorResult { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPostDoor()
    {
        DoorResult = Store.CheckDoorAccess(MemberCode);
        return Page();
    }

    public IActionResult OnPostAssignLocker()
    {
        Store.AssignLocker(LockerId, LockerMemberId, AssignedUntil);
        TempData["Message"] = "Locker assignment updated.";
        return RedirectToPage();
    }

    public IActionResult OnPostAddPromotion()
    {
        if (string.IsNullOrWhiteSpace(PromotionInput.Name))
        {
            TempData["Message"] = "Promotion name is required.";
            return RedirectToPage();
        }

        Store.AddPromotion(PromotionInput);
        TempData["Message"] = "Promotion plan added.";
        return RedirectToPage();
    }
}
