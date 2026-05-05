using ContratosIA.Models.Enums;

namespace ContratosIA.Models.Entities;

public class Contrato
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public TipoContrato Tipo { get; set; }
    public StatusContrato Status { get; set; } = StatusContrato.Rascunho;

    public string PrestadorNome { get; set; } = string.Empty;
    public string PrestadorCpfCnpj { get; set; } = string.Empty;
    public string PrestadorEndereco { get; set; } = string.Empty;

    public string ContratanteNome { get; set; } = string.Empty;
    public string ContratanteCpfCnpj { get; set; } = string.Empty;
    public string ContratanteEndereco { get; set; } = string.Empty;

    public string DescricaoServico { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Prazo { get; set; } = string.Empty;
    public string? ClausulasExtras { get; set; }

    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
