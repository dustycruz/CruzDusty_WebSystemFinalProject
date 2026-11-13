using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Web.Controllers;

// These actions only return page shells. All data is loaded by JavaScript from the API,
// and the API rejects every faculty request that does not carry a Faculty token.
public class FacultyController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    public IActionResult Grades()
    {
        return View();
    }
}