using ContratosIA.Models.Entities;

namespace ContratosIA.Models.ViewModels;

public class DashboardViewModel
{
    public string NomeUsuario { get; set; } = string.Empty;
    public string Plano { get; set; } = "FREE";
    public int TotalContratos { get; set; }
    public int ContratosMes { get; set; }
    public List<Contrato> Contratos { get; set; } = new();
}
