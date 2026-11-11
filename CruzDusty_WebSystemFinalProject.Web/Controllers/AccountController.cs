using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Web.Controllers;

public class AccountController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Login));
    }

    // The form on this page posts to the API with fetch (see wwwroot/js/auth.js).
    public IActionResult Login()
    {
        return View();
    }

    // Logout is done in the browser by clearing sessionStorage.
    // This action only exists so that typing /Account/Logout lands somewhere sensible.
    public IActionResult Logout()
    {
        return RedirectToAction(nameof(Login));
    }
}