using ContratosIA.Models.Entities;
using ContratosIA.Models.ViewModels;
using ContratosIA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ContratosIA.Controllers;

[Authorize]
public class ContratosController : Controller
{
    private readonly IContratoService _contratoService;
    private readonly IIAService _iaService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ContratosController> _logger;

    public ContratosController(IContratoService cs, IIAService ia, UserManager<ApplicationUser> um, ILogger<ContratosController> logger)
    {
        _contratoService = cs;
        _iaService = ia;
        _userManager = um;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Novo() => View(new ContratoWizardViewModel());

    [HttpPost]
    public async Task<IActionResult> Novo(ContratoWizardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning("ModelState inválido: {Errors}", string.Join("; ", errors));
            return View(model);
        }

        try
        {
            var user = (await _userManager.GetUserAsync(User))!;
            _logger.LogInformation("Gerando contrato tipo {Tipo} para usuário {UserId}", model.Tipo, user.Id);
            var conteudo = await _iaService.GerarContratoAsync(model.Tipo, model);
            _logger.LogInformation("Contrato gerado: {Length} caracteres", conteudo.Length);
            var contrato = await _contratoService.CriarAsync(user.Id, model, conteudo);

            return RedirectToAction(nameof(Detalhes), new { id = contrato.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar contrato: {Message}", ex.Message);
            ModelState.AddModelError("", $"Erro ao gerar contrato: {ex.Message}");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        var user = (await _userManager.GetUserAsync(User))!;
        var contrato = await _contratoService.ObterPorIdAsync(id, user.Id);
        if (contrato == null) return NotFound();
        return View(contrato);
    }

    [HttpPost]
    public async Task<IActionResult> Regerar(Guid id)
    {
        var user = (await _userManager.GetUserAsync(User))!;
        var contrato = await _contratoService.ObterPorIdAsync(id, user.Id);
        if (contrato == null) return NotFound();

        try
        {
            var wizard = new ContratoWizardViewModel
            {
                Tipo = contrato.Tipo,
                PrestadorNome = contrato.PrestadorNome,
                PrestadorCpfCnpj = contrato.PrestadorCpfCnpj,
                PrestadorEndereco = contrato.PrestadorEndereco,
                ContratanteNome = contrato.ContratanteNome,
                ContratanteCpfCnpj = contrato.ContratanteCpfCnpj,
                ContratanteEndereco = contrato.ContratanteEndereco,
                DescricaoServico = contrato.DescricaoServico,
                Valor = contrato.Valor,
                Prazo = contrato.Prazo,
                ClausulasExtras = contrato.ClausulasExtras
            };

            var novoConteudo = await _iaService.GerarContratoAsync(contrato.Tipo, wizard);
            await _contratoService.RegerarAsync(id, user.Id, novoConteudo);

            return RedirectToAction(nameof(Detalhes), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao regenerar contrato: {Message}", ex.Message);
            TempData["Erro"] = $"Erro ao regenerar contrato: {ex.Message}";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> BaixarPdf(Guid id)
    {
        var user = (await _userManager.GetUserAsync(User))!;
        var pdf = await _contratoService.GerarPdfAsync(id, user.Id);
        return File(pdf, "application/pdf", $"contrato-{id}.pdf");
    }
}
