namespace ContratosIA.Models.Enums;

public enum TipoContrato
{
    PrestacaoServico,
    DesenvolvimentoSoftware,
    Consultoria,
    Design,
    ParceriaComercial
}

public static class TipoContratoExtensions
{
    public static string ToLabel(this TipoContrato tipo) => tipo switch
    {
        TipoContrato.PrestacaoServico => "Prestação de Serviços",
        TipoContrato.DesenvolvimentoSoftware => "Desenvolvimento de Software",
        TipoContrato.Consultoria => "Consultoria",
        TipoContrato.Design => "Design / Criação",
        TipoContrato.ParceriaComercial => "Parceria Comercial",
        _ => tipo.ToString()
    };
}
