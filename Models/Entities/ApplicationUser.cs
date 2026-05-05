using Microsoft.AspNetCore.Identity;

namespace ContratosIA.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
    public string Plano { get; set; } = "FREE";
    public int ContratosMesAtual { get; set; } = 0;
    public DateTime MesResetEm { get; set; } = DateTime.UtcNow;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
