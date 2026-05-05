using ContratosIA.Models.Enums;
using ContratosIA.Models.ViewModels;

namespace ContratosIA.Services;

public interface IIAService
{
    Task<string> GerarContratoAsync(TipoContrato tipo, ContratoWizardViewModel dados);
}
