using ContratosIA.Models.ViewModels;
using ContratosIA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ContratosIA.Models.Entities;

namespace ContratosIA.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IContratoService _contratoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IContratoService cs, UserManager<ApplicationUser> um)
    {
        _contratoService = cs;
        _userManager = um;
    }

    public async Task<IActionResult> Index()
    {
        var user = (await _userManager.GetUserAsync(User))!;
        var contratos = await _contratoService.ListarPorUsuarioAsync(user.Id);
        var mes = DateTime.UtcNow.Month;
        var vm = new DashboardViewModel
        {
            NomeUsuario = user.Nome,
            Plano = user.Plano,
            TotalContratos = contratos.Count,
            ContratosMes = contratos.Count(c => c.CriadoEm.Month == mes),
            Contratos = contratos
        };
        return View(vm);
    }
}
