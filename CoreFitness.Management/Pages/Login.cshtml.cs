using CoreFitness.Management.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreFitness.Management.Pages;

public sealed class LoginModel : PageModel
{
    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectForRole();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = AppUsers.Validate(Username, Password);
        if (user is null)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AppUsers.CreatePrincipal(user),
            new AuthenticationProperties { IsPersistent = true });

        return user.Role == AppUsers.FuelBarRole
            ? RedirectToPage("/FuelBar")
            : RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }

    private IActionResult RedirectForRole()
    {
        return User.IsInRole(AppUsers.FuelBarRole) && !User.IsInRole(AppUsers.AdminRole)
            ? RedirectToPage("/FuelBar")
            : RedirectToPage("/Index");
    }
}
