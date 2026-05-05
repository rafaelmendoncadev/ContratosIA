using ContratosIA.Models.Entities;
using ContratosIA.Models.ViewModels;

namespace ContratosIA.Services;

public interface IContratoService
{
    Task<Contrato> CriarAsync(string userId, ContratoWizardViewModel dados, string conteudo);
    Task<List<Contrato>> ListarPorUsuarioAsync(string userId);
    Task<Contrato?> ObterPorIdAsync(Guid id, string userId);
    Task<Contrato> RegerarAsync(Guid id, string userId, string novoConteudo);
    Task<byte[]> GerarPdfAsync(Guid id, string userId);
}
