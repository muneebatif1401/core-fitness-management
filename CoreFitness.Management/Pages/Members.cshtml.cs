using CoreFitness.Management.Data;
using CoreFitness.Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

public sealed class MembersModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;

    [BindProperty]
    public Member Input { get; set; } = new() { MonthlyFee = 6000 };

    public void OnGet()
    {
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(Input.FullName) || string.IsNullOrWhiteSpace(Input.Phone))
        {
            TempData["Message"] = "Member name and phone are required.";
            return RedirectToPage();
        }

        Store.AddMember(Input);
        TempData["Message"] = "Member registered successfully.";
        return RedirectToPage();
    }

    public IActionResult OnPostRenew(Guid id, int months)
    {
        Store.RenewMember(id, months);
        TempData["Message"] = "Member fee renewed and door access restored.";
        return RedirectToPage();
    }

    public IActionResult OnPostToggleBlock(Guid id)
    {
        Store.ToggleMemberBlock(id);
        TempData["Message"] = "Member access status updated.";
        return RedirectToPage();
    }
}
