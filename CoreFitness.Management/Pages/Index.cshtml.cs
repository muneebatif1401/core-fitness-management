using Microsoft.AspNetCore.Mvc.RazorPages;
using CoreFitness.Management.Data;

namespace CoreFitness.Management.Pages;

public sealed class IndexModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;
}
