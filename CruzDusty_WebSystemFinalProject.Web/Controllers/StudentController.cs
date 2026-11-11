using Microsoft.AspNetCore.Mvc;

namespace CruzDusty_WebSystemFinalProject.Web.Controllers;

public class StudentController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(MyGrades));
    }

    public IActionResult MyGrades()
    {
        return View();
    }
}