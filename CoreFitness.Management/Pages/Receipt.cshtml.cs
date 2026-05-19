using CoreFitness.Management.Data;
using CoreFitness.Management.Models;
using CoreFitness.Management.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

[Authorize(Roles = "Admin,FuelBar")]
public sealed class ReceiptModel(GymStore store, IWebHostEnvironment environment) : PageModel
{
    public Sale? Sale { get; private set; }
    public bool AutoPrint { get; private set; }

    public IActionResult OnGet(Guid id, bool print = false)
    {
        Sale = store.GetSale(id);
        if (Sale is null)
        {
            return NotFound();
        }

        if (!CanAccess(Sale))
        {
            return Forbid();
        }

        AutoPrint = print;
        return Page();
    }

    public IActionResult OnGetPdf(Guid id)
    {
        var sale = store.GetSale(id);
        if (sale is null)
        {
            return NotFound();
        }

        if (!CanAccess(sale))
        {
            return Forbid();
        }

        var logoPath = Path.Combine(environment.WebRootPath, "img", "core-fitness-logo.png");
        var pdf = ReceiptPdfBuilder.Build(sale, logoPath);
        return File(pdf, "application/pdf", $"core-fitness-{sale.Outlet.ToLowerInvariant()}-{sale.Id.ToString()[..8]}.pdf");
    }

    private bool CanAccess(Sale sale)
    {
        return User.IsInRole("Admin") || sale.Outlet == "FuelBar";
    }
}
