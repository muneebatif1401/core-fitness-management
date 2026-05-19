using CoreFitness.Management.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

[Authorize(Roles = "Admin,FuelBar")]
public sealed class FuelBarModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;

    [BindProperty]
    public Guid ProductId { get; set; }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    [BindProperty]
    public string PaymentMethod { get; set; } = "Cash";

    public void OnGet()
    {
    }

    public IActionResult OnPostSell()
    {
        var soldBy = User.Identity?.Name ?? "Fuel Bar";
        var result = Store.RecordSale(ProductId, Quantity, soldBy, "FuelBar", PaymentMethod);
        if (result.Success && result.SaleId.HasValue)
        {
            return RedirectToPage("/Receipt", new { id = result.SaleId.Value, print = true });
        }

        TempData["Message"] = result.Message;
        return RedirectToPage();
    }
}
