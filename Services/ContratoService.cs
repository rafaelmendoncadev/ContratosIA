using ContratosIA.Data;
using ContratosIA.Models.Entities;
using ContratosIA.Models.Enums;
using ContratosIA.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ContratosIA.Services;

public class ContratoService : IContratoService
{
    private readonly ApplicationDbContext _db;

    public ContratoService(ApplicationDbContext db) => _db = db;

    public async Task<Contrato> CriarAsync(string userId, ContratoWizardViewModel dados, string conteudo)
    {
        var contrato = new Contrato
        {
            UserId = userId,
            Titulo = $"Contrato — {dados.PrestadorNome} × {dados.ContratanteNome}",
            Tipo = dados.Tipo,
            Status = StatusContrato.Gerado,
            PrestadorNome = dados.PrestadorNome,
            PrestadorCpfCnpj = dados.PrestadorCpfCnpj,
            PrestadorEndereco = dados.PrestadorEndereco,
            ContratanteNome = dados.ContratanteNome,
            ContratanteCpfCnpj = dados.ContratanteCpfCnpj,
            ContratanteEndereco = dados.ContratanteEndereco,
            DescricaoServico = dados.DescricaoServico,
            Valor = dados.Valor,
            Prazo = dados.Prazo,
            ClausulasExtras = dados.ClausulasExtras,
            Conteudo = conteudo
        };

        _db.Contratos.Add(contrato);
        await _db.SaveChangesAsync();
        return contrato;
    }

    public async Task<List<Contrato>> ListarPorUsuarioAsync(string userId) =>
        await _db.Contratos
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

    public async Task<Contrato?> ObterPorIdAsync(Guid id, string userId) =>
        await _db.Contratos.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    public async Task<Contrato> RegerarAsync(Guid id, string userId, string novoConteudo)
    {
        var contrato = await ObterPorIdAsync(id, userId)
            ?? throw new Exception("Contrato não encontrado");
        contrato.Conteudo = novoConteudo;
        contrato.AtualizadoEm = DateTime.UtcNow;
        contrato.Status = StatusContrato.Gerado;
        await _db.SaveChangesAsync();
        return contrato;
    }

    public async Task<byte[]> GerarPdfAsync(Guid id, string userId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var contrato = await ObterPorIdAsync(id, userId)
            ?? throw new Exception("Contrato não encontrado");

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(11).LineHeight(1.5f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("ContratoIA").FontSize(16).Bold().FontColor("#2563EB");
                        row.ConstantItem(150).AlignRight()
                           .Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(10).FontColor("#6B7280");
                    });
                    col.Item().PaddingTop(4).BorderBottom(1).BorderColor("#E5E7EB");
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().PaddingBottom(10).AlignCenter()
                       .Text($"CONTRATO DE {contrato.Tipo.ToLabel().ToUpper()}")
                       .Bold().FontSize(13);

                    col.Item().Text(contrato.Conteudo).FontFamily("Times New Roman").FontSize(11);
                });

                page.Footer().AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ").FontSize(9).FontColor("#9CA3AF");
                        x.CurrentPageNumber().FontSize(9).FontColor("#9CA3AF");
                        x.Span(" de ").FontSize(9).FontColor("#9CA3AF");
                        x.TotalPages().FontSize(9).FontColor("#9CA3AF");
                    });
            });
        });

        return pdf.GeneratePdf();
    }
}
