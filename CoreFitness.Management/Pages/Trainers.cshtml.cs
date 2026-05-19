using CoreFitness.Management.Data;
using CoreFitness.Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

[Authorize(Roles = "Admin")]
public sealed class TrainersModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;

    [BindProperty]
    public Trainer Input { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(Input.FullName) || string.IsNullOrWhiteSpace(Input.Phone))
        {
            TempData["Message"] = "Trainer name and phone are required.";
            return RedirectToPage();
        }

        Store.AddTrainer(Input);
        TempData["Message"] = "Trainer registered successfully.";
        return RedirectToPage();
    }
}
