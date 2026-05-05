using CoreFitness.Management.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

public sealed class POSModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;

    [BindProperty]
    public Guid ProductId { get; set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    [BindProperty]
    public string SoldBy { get; set; } = "Front Desk";

    public void OnGet()
    {
    }

    public IActionResult OnPostSell()
    {
        TempData["Message"] = Store.RecordSale(ProductId, Quantity, SoldBy);
        return RedirectToPage();
    }
}
