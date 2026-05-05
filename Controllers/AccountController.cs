using ContratosIA.Models.Entities;
using ContratosIA.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContratosIA.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm)
    {
        _userManager = um;
        _signInManager = sm;
    }

    [HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Senha, model.LembrarMe, false);
        if (result.Succeeded) return RedirectToAction("Index", "Dashboard");
        ModelState.AddModelError("", "E-mail ou senha inválidos.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Nome = model.Nome };
        var result = await _userManager.CreateAsync(user, model.Senha);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Dashboard");
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
