using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DoitBlazor.Models;

namespace DoitBlazor.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LogoutModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGet()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("~/");
    }

    public async Task<IActionResult> OnPost()
    {
        await _signInManager.SignOutAsync();
        return LocalRedirect("~/");
    }
}
