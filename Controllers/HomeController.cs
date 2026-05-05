using Microsoft.AspNetCore.Mvc;

namespace ContratosIA.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
