using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Web.Controllers;

public class HomeController : Controller
{
    // "/" : auth.js sends the visitor to the login page or to the home page of their role.
    public IActionResult Index()
    {
        return View();
    }

    // "/Home/AccessDenied" : shown when a signed-in user opens a page for another role.
    public IActionResult AccessDenied()
    {
        return View("AccessDenied");
    }

    // Target of UseStatusCodePagesWithReExecute.
    public IActionResult HttpStatus(int code = 404)
    {
        if (code == 403)
        {
            return View("AccessDenied");
        }

        if (code == 404)
        {
            ViewData["StatusCode"] = "404";
            ViewData["StatusHeading"] = "Page Not Found";
            ViewData["StatusMessage"] = "The page you are looking for does not exist or has been moved.";
        }
        else
        {
            ViewData["StatusCode"] = code.ToString();
            ViewData["StatusHeading"] = "Something Went Wrong";
            ViewData["StatusMessage"] = "The request could not be completed. Please go back and try again.";
        }

        return View("NotFound");
    }

    // Target of UseExceptionHandler outside Development.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["StatusCode"] = "500";
        ViewData["StatusHeading"] = "Something Went Wrong";
        ViewData["StatusMessage"] = "An unexpected error occurred. Please go back and try again.";

        return View("NotFound");
    }
}