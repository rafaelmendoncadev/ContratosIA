using System.ComponentModel.DataAnnotations;
using ContratosIA.Models.Enums;

namespace ContratosIA.Models.ViewModels;

public class ContratoWizardViewModel
{
    public TipoContrato Tipo { get; set; }

    [Required] public string PrestadorNome { get; set; } = string.Empty;
    [Required] public string PrestadorCpfCnpj { get; set; } = string.Empty;
    [Required] public string PrestadorEndereco { get; set; } = string.Empty;

    [Required] public string ContratanteNome { get; set; } = string.Empty;
    [Required] public string ContratanteCpfCnpj { get; set; } = string.Empty;
    [Required] public string ContratanteEndereco { get; set; } = string.Empty;

    [Required] public string DescricaoServico { get; set; } = string.Empty;
    [Required] public decimal Valor { get; set; }
    [Required] public string Prazo { get; set; } = string.Empty;
    public string? ClausulasExtras { get; set; }

    public int StepAtual { get; set; } = 1;
}
