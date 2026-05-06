using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ContratosIA.Models.Enums;
using ContratosIA.Models.ViewModels;

namespace ContratosIA.Services;

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly ILogger<IAService> _logger;

    public IAService(HttpClient httpClient, IConfiguration config, ILogger<IAService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = string.IsNullOrWhiteSpace(config["OpenRouter:ApiKey"])
            ? throw new Exception("Chave API do OpenRouter não configurada. Defina OpenRouter__ApiKey como variável de ambiente ou em appsettings.Development.json.")
            : config["OpenRouter:ApiKey"]!;
        _model = config["OpenRouter:Model"] ?? "deepseek/deepseek-v4-pro";
        _baseUrl = config["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://contratoia.app");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "ContratoIA");
    }

    public async Task<string> GerarContratoAsync(TipoContrato tipo, ContratoWizardViewModel dados)
    {
        var prompt = MontarPrompt(tipo, dados);

        var requestBody = new
        {
            model = _model,
            max_tokens = 8192,
            temperature = 0.7,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"{_baseUrl}/chat/completions";

        var maxRetries = 5;
        var delays = new[] { 5, 15, 30, 60 };
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
                break;

            if ((response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                && attempt < maxRetries)
            {
                var delaySeconds = delays[attempt - 1];
                var errorInfo = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Rate limit OpenRouter (tentativa {Attempt}/{Max}, aguardando {Delay}s): {Body}",
                    attempt, maxRetries, delaySeconds, errorInfo);

                if (response.Headers.RetryAfter is { } retryAfter)
                    delaySeconds = (int)(retryAfter.Delta ?? TimeSpan.FromSeconds(delaySeconds)).TotalSeconds;

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                continue;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro da API OpenRouter: {StatusCode} - {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"API OpenRouter retornou {(int)response.StatusCode} ({response.StatusCode}). " +
                $"Resposta: {errorBody}");
        }

        var responseJson = await response!.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private string MontarPrompt(TipoContrato tipo, ContratoWizardViewModel d)
    {
        var tipoLabel = tipo.ToLabel();
        var clausulasExtras = string.IsNullOrWhiteSpace(d.ClausulasExtras)
            ? "Nenhuma cláusula extra especificada."
            : d.ClausulasExtras;

        return $"""
            Você é um assistente jurídico brasileiro especializado em contratos.
            Gere um contrato profissional completo do tipo: {tipoLabel}.

            DADOS DO PRESTADOR:
            Nome: {d.PrestadorNome}
            CPF/CNPJ: {d.PrestadorCpfCnpj}
            Endereço: {d.PrestadorEndereco}

            DADOS DO CONTRATANTE:
            Nome: {d.ContratanteNome}
            CPF/CNPJ: {d.ContratanteCpfCnpj}
            Endereço: {d.ContratanteEndereco}

            OBJETO DO CONTRATO:
            Descrição: {d.DescricaoServico}
            Valor: R$ {d.Valor:N2}
            Prazo: {d.Prazo}
            Cláusulas extras: {clausulasExtras}

            INSTRUÇÕES:
            - Redija em português brasileiro formal e jurídico.
            - Inclua obrigatoriamente as cláusulas: Objeto, Obrigações das Partes, Pagamento, Prazo, Rescisão e Foro (Brasília/DF).
            - Para contratos de Desenvolvimento de Software, inclua cláusulas de Propriedade Intelectual.
            - Para contratos de Design, inclua cláusula de Cessão de Uso.
            - Para Parceria Comercial, inclua cláusula de Divisão de Receita.
            - Formate com CLÁUSULAS numeradas (CLÁUSULA PRIMEIRA, SEGUNDA, etc.).
            - Inclua local e data para assinaturas ao final.
            - Retorne APENAS o texto do contrato, sem explicações adicionais.
            """;
    }
}
