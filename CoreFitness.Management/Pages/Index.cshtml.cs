using Microsoft.AspNetCore.Mvc.RazorPages;
using CoreFitness.Management.Data;
using Microsoft.AspNetCore.Authorization;

namespace CoreFitness.Management.Pages;

[Authorize(Roles = "Admin")]
public sealed class IndexModel(GymStore store) : PageModel
{
    public GymStore Store { get; } = store;
}
