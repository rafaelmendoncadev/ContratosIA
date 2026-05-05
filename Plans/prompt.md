# PROMPT DE AGENTE — ContratoIA (ASP.NET Core MVC)

Você é um agente de desenvolvimento especializado em ASP.NET Core MVC. Sua tarefa é construir do zero o sistema **ContratoIA** — uma plataforma web SaaS que permite usuários gerarem contratos jurídicos profissionais via IA.

---

## Stack obrigatória

- **Framework:** ASP.NET Core 8 MVC
- **ORM:** Entity Framework Core 8 + Npgsql (PostgreSQL)
- **Banco de dados:** PostgreSQL (Neon — configurado via connection string em appsettings)
- **Autenticação:** ASP.NET Core Identity
- **IA:** API da Anthropic (Claude claude-sonnet-4-20250514) via HttpClient
- **PDF:** iTextSharp ou QuestPDF (geração server-side)
- **Frontend:** Razor Views + Tailwind CSS (via CDN Play) + Alpine.js (interatividade)
- **Hosting target:** Render.com

---

## Estrutura do projeto

```
ContratoIA/
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── DashboardController.cs
│   └── ContratosController.cs
├── Models/
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   └── Contrato.cs
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   ├── ContratoWizardViewModel.cs
│   │   └── ContratoDetalheViewModel.cs
│   └── Enums/
│       ├── TipoContrato.cs
│       └── StatusContrato.cs
├── Services/
│   ├── IContratoService.cs
│   ├── ContratoService.cs
│   ├── IIAService.cs
│   └── IAService.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _SiteHeader.cshtml
│   │   └── _ValidationScripts.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Account/
│   │   ├── Login.cshtml
│   │   └── Register.cshtml
│   ├── Dashboard/
│   │   └── Index.cshtml
│   └── Contratos/
│       ├── Novo.cshtml
│       └── Detalhes.cshtml
├── wwwroot/
│   └── (arquivos estáticos)
├── Program.cs
└── appsettings.json
```

---

## 1. ENUMS

### `Models/Enums/TipoContrato.cs`
```csharp
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
```

### `Models/Enums/StatusContrato.cs`
```csharp
public enum StatusContrato
{
    Rascunho,
    Gerado,
    Assinado
}
```

---

## 2. ENTITIES

### `Models/Entities/ApplicationUser.cs`
```csharp
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
    public string Plano { get; set; } = "FREE"; // FREE | PRO | AGENCY
    public int ContratosMesAtual { get; set; } = 0;
    public DateTime MesResetEm { get; set; } = DateTime.UtcNow;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<Contrato> Contratos { get; set; } = new List<Contrato>();
}
```

### `Models/Entities/Contrato.cs`
```csharp
public class Contrato
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public TipoContrato Tipo { get; set; }
    public StatusContrato Status { get; set; } = StatusContrato.Rascunho;

    // Dados do Prestador
    public string PrestadorNome { get; set; } = string.Empty;
    public string PrestadorCpfCnpj { get; set; } = string.Empty;
    public string PrestadorEndereco { get; set; } = string.Empty;

    // Dados do Contratante
    public string ContratanteNome { get; set; } = string.Empty;
    public string ContratanteCpfCnpj { get; set; } = string.Empty;
    public string ContratanteEndereco { get; set; } = string.Empty;

    // Objeto do contrato
    public string DescricaoServico { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Prazo { get; set; } = string.Empty;
    public string? ClausulasExtras { get; set; }

    // Conteúdo gerado
    public string Conteudo { get; set; } = string.Empty;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
```

---

## 3. DbContext

### `Data/ApplicationDbContext.cs`
```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Contrato> Contratos { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Contrato>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Valor).HasColumnType("decimal(18,2)");
            e.Property(c => c.Tipo).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
            e.HasOne(c => c.User)
             .WithMany(u => u.Contratos)
             .HasForeignKey(c => c.UserId);
        });
    }
}
```

---

## 4. VIEWMODELS

### `Models/ViewModels/LoginViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha obrigatória")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    public bool LembrarMe { get; set; }
}
```

### `Models/ViewModels/RegisterViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Nome obrigatório")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha obrigatória")]
    [MinLength(6, ErrorMessage = "Mínimo 6 caracteres")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Compare("Senha", ErrorMessage = "Senhas não conferem")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
```

### `Models/ViewModels/DashboardViewModel.cs`
```csharp
public class DashboardViewModel
{
    public string NomeUsuario { get; set; } = string.Empty;
    public string Plano { get; set; } = "FREE";
    public int TotalContratos { get; set; }
    public int ContratosMes { get; set; }
    public List<Contrato> Contratos { get; set; } = new();
}
```

### `Models/ViewModels/ContratoWizardViewModel.cs`
```csharp
using System.ComponentModel.DataAnnotations;

public class ContratoWizardViewModel
{
    // Step 1
    public TipoContrato Tipo { get; set; }

    // Step 2 — Prestador
    [Required] public string PrestadorNome { get; set; } = string.Empty;
    [Required] public string PrestadorCpfCnpj { get; set; } = string.Empty;
    [Required] public string PrestadorEndereco { get; set; } = string.Empty;

    // Step 3 — Contratante
    [Required] public string ContratanteNome { get; set; } = string.Empty;
    [Required] public string ContratanteCpfCnpj { get; set; } = string.Empty;
    [Required] public string ContratanteEndereco { get; set; } = string.Empty;

    // Step 4 — Objeto
    [Required] public string DescricaoServico { get; set; } = string.Empty;
    [Required] public decimal Valor { get; set; }
    [Required] public string Prazo { get; set; } = string.Empty;
    public string? ClausulasExtras { get; set; }

    public int StepAtual { get; set; } = 1;
}
```

---

## 5. SERVICES

### `Services/IIAService.cs`
```csharp
public interface IIAService
{
    Task<string> GerarContratoAsync(TipoContrato tipo, ContratoWizardViewModel dados);
}
```

### `Services/IAService.cs`

Integração com a API da Anthropic (Claude). Usar HttpClient com a chave em appsettings.

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class IAService : IIAService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public IAService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Anthropic:ApiKey"] ?? throw new Exception("Chave API não configurada");
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GerarContratoAsync(TipoContrato tipo, ContratoWizardViewModel dados)
    {
        var prompt = MontarPrompt(tipo, dados);

        var requestBody = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/v1/messages", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
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
```

### `Services/IContratoService.cs` e `ContratoService.cs`

```csharp
public interface IContratoService
{
    Task<Contrato> CriarAsync(string userId, ContratoWizardViewModel dados, string conteudo);
    Task<List<Contrato>> ListarPorUsuarioAsync(string userId);
    Task<Contrato?> ObterPorIdAsync(Guid id, string userId);
    Task<Contrato> RegerarAsync(Guid id, string userId, string novoConteudo);
    Task<byte[]> GerarPdfAsync(Guid id, string userId);
}

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
        var contrato = await ObterPorIdAsync(id, userId)
            ?? throw new Exception("Contrato não encontrado");

        // Implementar geração PDF com QuestPDF
        // Instalar: dotnet add package QuestPDF
        // Ver seção de PDF abaixo
        throw new NotImplementedException("Implementar com QuestPDF");
    }
}
```

---

## 6. CONTROLLERS

### `Controllers/HomeController.cs`
```csharp
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
```

### `Controllers/AccountController.cs`
```csharp
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> um, SignInManager<ApplicationUser> sm)
    {
        _userManager = um;
        _signInManager = sm;
    }

    [HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Senha, model.LembrarMe, false);
        if (result.Succeeded) return RedirectToAction("Index", "Dashboard");
        ModelState.AddModelError("", "E-mail ou senha inválidos.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Nome = model.Nome };
        var result = await _userManager.CreateAsync(user, model.Senha);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Index", "Dashboard");
        }
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error.Description);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
```

### `Controllers/DashboardController.cs`
```csharp
[Authorize]
public class DashboardController : Controller
{
    private readonly IContratoService _contratoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IContratoService cs, UserManager<ApplicationUser> um)
    {
        _contratoService = cs;
        _userManager = um;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User)!;
        var contratos = await _contratoService.ListarPorUsuarioAsync(user!.Id);
        var mes = DateTime.UtcNow.Month;
        var vm = new DashboardViewModel
        {
            NomeUsuario = user.Nome,
            Plano = user.Plano,
            TotalContratos = contratos.Count,
            ContratosMes = contratos.Count(c => c.CriadoEm.Month == mes),
            Contratos = contratos
        };
        return View(vm);
    }
}
```

### `Controllers/ContratosController.cs`
```csharp
[Authorize]
public class ContratosController : Controller
{
    private readonly IContratoService _contratoService;
    private readonly IIAService _iaService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContratosController(IContratoService cs, IIAService ia, UserManager<ApplicationUser> um)
    {
        _contratoService = cs;
        _iaService = ia;
        _userManager = um;
    }

    [HttpGet]
    public IActionResult Novo() => View(new ContratoWizardViewModel());

    [HttpPost]
    public async Task<IActionResult> Novo(ContratoWizardViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User)!;
        var conteudo = await _iaService.GerarContratoAsync(model.Tipo, model);
        var contrato = await _contratoService.CriarAsync(user!.Id, model, conteudo);

        return RedirectToAction(nameof(Detalhes), new { id = contrato.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        var user = await _userManager.GetUserAsync(User)!;
        var contrato = await _contratoService.ObterPorIdAsync(id, user!.Id);
        if (contrato == null) return NotFound();
        return View(contrato);
    }

    [HttpPost]
    public async Task<IActionResult> Regerar(Guid id)
    {
        var user = await _userManager.GetUserAsync(User)!;
        var contrato = await _contratoService.ObterPorIdAsync(id, user!.Id);
        if (contrato == null) return NotFound();

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
        await _contratoService.RegerarAsync(id, user!.Id, novoConteudo);

        return RedirectToAction(nameof(Detalhes), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> BaixarPdf(Guid id)
    {
        var user = await _userManager.GetUserAsync(User)!;
        var pdf = await _contratoService.GerarPdfAsync(id, user!.Id);
        return File(pdf, "application/pdf", $"contrato-{id}.pdf");
    }
}
```

---

## 7. VIEWS

### `Views/Shared/_Layout.cshtml`

Layout base com Tailwind CSS (CDN), Alpine.js e SiteHeader.

```html
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — ContratoIA</title>
    <script src="https://cdn.tailwindcss.com"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.x.x/dist/cdn.min.js"></script>
    <style>
        [x-cloak] { display: none !important; }
        .font-serif { font-family: 'Times New Roman', Times, serif; }
    </style>
</head>
<body class="bg-gray-50 text-gray-900 min-h-screen flex flex-col">
    <partial name="_SiteHeader" />
    <main class="flex-1">
        @RenderBody()
    </main>
    <footer class="bg-white border-t py-6 text-center text-sm text-gray-500">
        © @DateTime.Now.Year ContratoIA — Contratos jurídicos gerados por IA
    </footer>
    @RenderSection("Scripts", required: false)
</body>
</html>
```

### `Views/Shared/_SiteHeader.cshtml`

```html
<header class="bg-white border-b shadow-sm">
    <div class="max-w-6xl mx-auto px-4 py-4 flex items-center justify-between">
        <a href="/" class="text-2xl font-bold text-blue-600">ContratoIA</a>
        <nav class="flex gap-4 items-center">
            @if (User.Identity?.IsAuthenticated == true)
            {
                <a asp-controller="Dashboard" asp-action="Index"
                   class="text-gray-600 hover:text-blue-600 font-medium">Dashboard</a>
                <form asp-controller="Account" asp-action="Logout" method="post" class="inline">
                    <button type="submit"
                            class="text-gray-600 hover:text-red-500 font-medium">Sair</button>
                </form>
            }
            else
            {
                <a asp-controller="Account" asp-action="Login"
                   class="text-gray-600 hover:text-blue-600 font-medium">Entrar</a>
                <a asp-controller="Account" asp-action="Register"
                   class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 font-medium">
                    Começar grátis
                </a>
            }
        </nav>
    </div>
</header>
```

### `Views/Home/Index.cshtml` — Landing Page

```html
@{
    ViewData["Title"] = "Contratos jurídicos profissionais em minutos";
    Layout = "_Layout";
}

<!-- Hero -->
<section class="bg-gradient-to-br from-blue-600 to-blue-800 text-white py-24">
    <div class="max-w-4xl mx-auto px-4 text-center">
        <h1 class="text-5xl font-bold mb-6">Gere contratos jurídicos profissionais em minutos</h1>
        <p class="text-xl text-blue-100 mb-10">
            Powered by IA. Sem advogados. Sem complicação.
            Prestação de serviços, software, consultoria e muito mais.
        </p>
        <a asp-controller="Account" asp-action="Register"
           class="bg-white text-blue-700 px-8 py-4 rounded-xl font-bold text-lg hover:bg-blue-50 inline-block">
            Começar grátis
        </a>
    </div>
</section>

<!-- Benefícios -->
<section class="py-20 max-w-6xl mx-auto px-4">
    <h2 class="text-3xl font-bold text-center mb-12">Por que usar o ContratoIA?</h2>
    <div class="grid md:grid-cols-3 gap-8">
        <div class="bg-white p-6 rounded-xl shadow-sm border text-center">
            <div class="text-4xl mb-4">⚡</div>
            <h3 class="text-xl font-semibold mb-2">Rápido</h3>
            <p class="text-gray-600">Contrato completo gerado em menos de 1 minuto</p>
        </div>
        <div class="bg-white p-6 rounded-xl shadow-sm border text-center">
            <div class="text-4xl mb-4">⚖️</div>
            <h3 class="text-xl font-semibold mb-2">Jurídico</h3>
            <p class="text-gray-600">Cláusulas obrigatórias brasileiras incluídas automaticamente</p>
        </div>
        <div class="bg-white p-6 rounded-xl shadow-sm border text-center">
            <div class="text-4xl mb-4">📄</div>
            <h3 class="text-xl font-semibold mb-2">PDF profissional</h3>
            <p class="text-gray-600">Baixe em PDF pronto para assinar</p>
        </div>
    </div>
</section>

<!-- Tipos de contrato -->
<section class="bg-white py-16">
    <div class="max-w-4xl mx-auto px-4 text-center">
        <h2 class="text-3xl font-bold mb-8">Tipos de contrato disponíveis</h2>
        <div class="flex flex-wrap justify-center gap-3">
            @foreach (var tipo in Enum.GetValues<TipoContrato>())
            {
                <span class="bg-blue-50 text-blue-700 px-4 py-2 rounded-full font-medium">
                    @tipo.ToLabel()
                </span>
            }
        </div>
    </div>
</section>

<!-- CTA Final -->
<section class="py-20 text-center bg-gray-50">
    <h2 class="text-3xl font-bold mb-4">Pronto para criar seu primeiro contrato?</h2>
    <p class="text-gray-600 mb-8">Grátis. Sem cartão de crédito.</p>
    <a asp-controller="Account" asp-action="Register"
       class="bg-blue-600 text-white px-8 py-4 rounded-xl font-bold text-lg hover:bg-blue-700 inline-block">
        Criar conta grátis
    </a>
</section>
```

### `Views/Account/Login.cshtml`

```html
@model LoginViewModel
@{
    ViewData["Title"] = "Entrar";
    Layout = "_Layout";
}

<div class="min-h-screen bg-gradient-to-br from-blue-50 to-gray-100 flex items-center justify-center py-12 px-4">
    <div class="bg-white w-full max-w-md p-8 rounded-2xl shadow-lg">
        <h1 class="text-2xl font-bold text-center text-gray-800 mb-8">Entrar no ContratoIA</h1>

        <div asp-validation-summary="All" class="bg-red-50 text-red-600 p-3 rounded-lg mb-4 text-sm"></div>

        <form asp-action="Login" method="post" class="space-y-4">
            @Html.AntiForgeryToken()
            <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">E-mail</label>
                <input asp-for="Email" type="email"
                       class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Senha</label>
                <input asp-for="Senha" type="password"
                       class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
            </div>
            <div class="flex items-center gap-2">
                <input asp-for="LembrarMe" type="checkbox" class="rounded" />
                <label asp-for="LembrarMe" class="text-sm text-gray-600">Lembrar-me</label>
            </div>
            <button type="submit"
                    class="w-full bg-blue-600 text-white py-3 rounded-lg font-semibold hover:bg-blue-700 transition">
                Entrar
            </button>
        </form>

        <p class="text-center text-sm text-gray-600 mt-6">
            Não tem conta?
            <a asp-controller="Account" asp-action="Register" class="text-blue-600 font-medium hover:underline">Criar conta</a>
        </p>
    </div>
</div>
```

### `Views/Account/Register.cshtml`

Mesma estrutura do Login, com campos Nome, Email, Senha, ConfirmarSenha. Adapte.

### `Views/Dashboard/Index.cshtml`

```html
@model DashboardViewModel
@{
    ViewData["Title"] = "Dashboard";
    Layout = "_Layout";
}

<div class="max-w-6xl mx-auto px-4 py-10">
    <div class="flex items-center justify-between mb-8">
        <div>
            <h1 class="text-2xl font-bold text-gray-800">Olá, @Model.NomeUsuario 👋</h1>
            <p class="text-gray-500">Plano atual: <span class="font-semibold text-blue-600">@Model.Plano</span></p>
        </div>
        <a asp-controller="Contratos" asp-action="Novo"
           class="bg-blue-600 text-white px-6 py-3 rounded-lg font-semibold hover:bg-blue-700">
            + Novo contrato
        </a>
    </div>

    <!-- Métricas -->
    <div class="grid md:grid-cols-3 gap-6 mb-10">
        <div class="bg-white p-6 rounded-xl border shadow-sm text-center">
            <p class="text-3xl font-bold text-blue-600">@Model.TotalContratos</p>
            <p class="text-gray-500 text-sm mt-1">Total de contratos</p>
        </div>
        <div class="bg-white p-6 rounded-xl border shadow-sm text-center">
            <p class="text-3xl font-bold text-green-600">@Model.ContratosMes</p>
            <p class="text-gray-500 text-sm mt-1">Contratos este mês</p>
        </div>
        <div class="bg-white p-6 rounded-xl border shadow-sm text-center">
            <p class="text-3xl font-bold text-purple-600">@Model.Plano</p>
            <p class="text-gray-500 text-sm mt-1">Plano ativo</p>
        </div>
    </div>

    <!-- Lista de contratos -->
    @if (!Model.Contratos.Any())
    {
        <div class="text-center py-16 text-gray-400">
            <p class="text-5xl mb-4">📄</p>
            <p class="text-lg">Nenhum contrato ainda.</p>
            <a asp-controller="Contratos" asp-action="Novo"
               class="text-blue-600 font-medium hover:underline mt-2 inline-block">
                Criar meu primeiro contrato
            </a>
        </div>
    }
    else
    {
        <div class="bg-white rounded-xl border shadow-sm overflow-hidden">
            <table class="w-full text-sm">
                <thead class="bg-gray-50 border-b">
                    <tr>
                        <th class="text-left px-6 py-4 font-medium text-gray-600">Título</th>
                        <th class="text-left px-6 py-4 font-medium text-gray-600">Tipo</th>
                        <th class="text-left px-6 py-4 font-medium text-gray-600">Status</th>
                        <th class="text-left px-6 py-4 font-medium text-gray-600">Data</th>
                        <th class="px-6 py-4"></th>
                    </tr>
                </thead>
                <tbody class="divide-y">
                    @foreach (var c in Model.Contratos)
                    {
                        <tr class="hover:bg-gray-50">
                            <td class="px-6 py-4 font-medium">@c.Titulo</td>
                            <td class="px-6 py-4 text-gray-600">@c.Tipo.ToLabel()</td>
                            <td class="px-6 py-4">
                                <span class="px-2 py-1 rounded-full text-xs font-medium
                                    @(c.Status == StatusContrato.Gerado ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-600")">
                                    @c.Status
                                </span>
                            </td>
                            <td class="px-6 py-4 text-gray-500">@c.CriadoEm.ToString("dd/MM/yyyy")</td>
                            <td class="px-6 py-4">
                                <a asp-controller="Contratos" asp-action="Detalhes" asp-route-id="@c.Id"
                                   class="text-blue-600 hover:underline font-medium">Ver</a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    }
</div>
```

### `Views/Contratos/Novo.cshtml` — Wizard 4 Steps (Alpine.js)

```html
@model ContratoWizardViewModel
@{
    ViewData["Title"] = "Novo Contrato";
    Layout = "_Layout";
}

<div class="max-w-3xl mx-auto px-4 py-10" x-data="wizard()">

    <!-- Progress bar -->
    <div class="mb-8">
        <div class="flex justify-between text-sm text-gray-500 mb-2">
            <span>Passo <span x-text="step"></span> de 4</span>
            <span x-text="stepLabel()"></span>
        </div>
        <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-blue-600 h-2 rounded-full transition-all" :style="`width: ${(step/4)*100}%`"></div>
        </div>
    </div>

    <form asp-action="Novo" method="post" id="wizardForm">
        @Html.AntiForgeryToken()

        <!-- STEP 1: Tipo -->
        <div x-show="step === 1" x-cloak class="bg-white p-8 rounded-2xl shadow-sm border">
            <h2 class="text-xl font-bold mb-6">Qual tipo de contrato?</h2>
            <div class="grid md:grid-cols-2 gap-4">
                @foreach (var tipo in Enum.GetValues<TipoContrato>())
                {
                    <label class="cursor-pointer">
                        <input type="radio" asp-for="Tipo" value="@tipo"
                               x-model="formData.Tipo" class="sr-only peer" />
                        <div class="border-2 rounded-xl p-4 peer-checked:border-blue-500 peer-checked:bg-blue-50 hover:border-gray-300 transition">
                            <p class="font-medium">@tipo.ToLabel()</p>
                        </div>
                    </label>
                }
            </div>
            <div class="flex justify-end mt-6">
                <button type="button" @@click="nextStep()"
                        class="bg-blue-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-blue-700">
                    Próximo →
                </button>
            </div>
        </div>

        <!-- STEP 2: Prestador -->
        <div x-show="step === 2" x-cloak class="bg-white p-8 rounded-2xl shadow-sm border">
            <h2 class="text-xl font-bold mb-6">Dados do Prestador</h2>
            <div class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">Nome completo / Razão Social</label>
                    <input asp-for="PrestadorNome" x-model="formData.PrestadorNome"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">CPF / CNPJ</label>
                    <input asp-for="PrestadorCpfCnpj" x-model="formData.PrestadorCpfCnpj"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">Endereço completo</label>
                    <input asp-for="PrestadorEndereco" x-model="formData.PrestadorEndereco"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
            </div>
            <div class="flex justify-between mt-6">
                <button type="button" @@click="prevStep()"
                        class="text-gray-600 px-6 py-2.5 rounded-lg font-medium hover:bg-gray-100">← Voltar</button>
                <button type="button" @@click="nextStep()"
                        class="bg-blue-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-blue-700">Próximo →</button>
            </div>
        </div>

        <!-- STEP 3: Contratante -->
        <div x-show="step === 3" x-cloak class="bg-white p-8 rounded-2xl shadow-sm border">
            <h2 class="text-xl font-bold mb-6">Dados do Contratante</h2>
            <div class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">Nome completo / Razão Social</label>
                    <input asp-for="ContratanteNome" x-model="formData.ContratanteNome"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">CPF / CNPJ</label>
                    <input asp-for="ContratanteCpfCnpj" x-model="formData.ContratanteCpfCnpj"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">Endereço completo</label>
                    <input asp-for="ContratanteEndereco" x-model="formData.ContratanteEndereco"
                           class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                </div>
            </div>
            <div class="flex justify-between mt-6">
                <button type="button" @@click="prevStep()"
                        class="text-gray-600 px-6 py-2.5 rounded-lg font-medium hover:bg-gray-100">← Voltar</button>
                <button type="button" @@click="nextStep()"
                        class="bg-blue-600 text-white px-6 py-2.5 rounded-lg font-medium hover:bg-blue-700">Próximo →</button>
            </div>
        </div>

        <!-- STEP 4: Objeto -->
        <div x-show="step === 4" x-cloak class="bg-white p-8 rounded-2xl shadow-sm border">
            <h2 class="text-xl font-bold mb-6">Objeto do contrato</h2>
            <div class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">
                        Descrição do serviço
                        <span class="text-gray-400 font-normal">(máx. 1000 caracteres)</span>
                    </label>
                    <textarea asp-for="DescricaoServico" x-model="formData.DescricaoServico"
                              rows="5" maxlength="1000"
                              class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none resize-none"></textarea>
                    <p class="text-right text-xs text-gray-400 mt-1">
                        <span x-text="formData.DescricaoServico?.length ?? 0"></span>/1000
                    </p>
                </div>
                <div class="grid md:grid-cols-2 gap-4">
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Valor (R$)</label>
                        <input asp-for="Valor" type="number" step="0.01" x-model="formData.Valor"
                               class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 mb-1">Prazo</label>
                        <input asp-for="Prazo" x-model="formData.Prazo" placeholder="Ex: 30 dias, 3 meses"
                               class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none" />
                    </div>
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-1">Cláusulas extras (opcional)</label>
                    <textarea asp-for="ClausulasExtras" x-model="formData.ClausulasExtras"
                              rows="3"
                              class="w-full border rounded-lg px-4 py-2.5 focus:ring-2 focus:ring-blue-500 focus:outline-none resize-none"></textarea>
                </div>
            </div>
            <div class="flex justify-between mt-6">
                <button type="button" @@click="prevStep()"
                        class="text-gray-600 px-6 py-2.5 rounded-lg font-medium hover:bg-gray-100">← Voltar</button>
                <button type="submit"
                        class="bg-green-600 text-white px-8 py-2.5 rounded-lg font-semibold hover:bg-green-700 flex items-center gap-2">
                    <span>✨ Gerar contrato com IA</span>
                </button>
            </div>
        </div>
    </form>
</div>

@section Scripts {
<script>
function wizard() {
    return {
        step: 1,
        formData: { Tipo: '', PrestadorNome: '', PrestadorCpfCnpj: '', PrestadorEndereco: '',
                     ContratanteNome: '', ContratanteCpfCnpj: '', ContratanteEndereco: '',
                     DescricaoServico: '', Valor: '', Prazo: '', ClausulasExtras: '' },
        stepLabel() {
            return ['Tipo de contrato', 'Dados do Prestador', 'Dados do Contratante', 'Objeto e valor'][this.step - 1];
        },
        nextStep() { if (this.step < 4) this.step++; },
        prevStep() { if (this.step > 1) this.step--; }
    }
}
</script>
}
```

### `Views/Contratos/Detalhes.cshtml`

```html
@model Contrato
@{
    ViewData["Title"] = Model.Titulo;
    Layout = "_Layout";
}

<div class="max-w-6xl mx-auto px-4 py-10">
    <div class="flex gap-8">

        <!-- Sidebar -->
        <aside class="w-72 shrink-0">
            <div class="bg-white p-6 rounded-xl border shadow-sm mb-4">
                <h2 class="font-bold text-gray-800 mb-4">Detalhes</h2>
                <dl class="space-y-3 text-sm">
                    <div>
                        <dt class="text-gray-500">Tipo</dt>
                        <dd class="font-medium">@Model.Tipo.ToLabel()</dd>
                    </div>
                    <div>
                        <dt class="text-gray-500">Prestador</dt>
                        <dd class="font-medium">@Model.PrestadorNome</dd>
                    </div>
                    <div>
                        <dt class="text-gray-500">Contratante</dt>
                        <dd class="font-medium">@Model.ContratanteNome</dd>
                    </div>
                    <div>
                        <dt class="text-gray-500">Valor</dt>
                        <dd class="font-medium">@Model.Valor.ToString("C", new System.Globalization.CultureInfo("pt-BR"))</dd>
                    </div>
                    <div>
                        <dt class="text-gray-500">Prazo</dt>
                        <dd class="font-medium">@Model.Prazo</dd>
                    </div>
                    <div>
                        <dt class="text-gray-500">Status</dt>
                        <dd>
                            <span class="px-2 py-1 bg-green-100 text-green-700 rounded-full text-xs font-medium">
                                @Model.Status
                            </span>
                        </dd>
                    </div>
                </dl>
            </div>

            <!-- Ações -->
            <div class="space-y-2">
                <a asp-controller="Contratos" asp-action="BaixarPdf" asp-route-id="@Model.Id"
                   class="block w-full bg-blue-600 text-white px-4 py-3 rounded-lg font-medium text-center hover:bg-blue-700">
                    📥 Baixar PDF
                </a>
                <button onclick="copiarTexto()"
                        class="block w-full border border-gray-300 text-gray-700 px-4 py-3 rounded-lg font-medium text-center hover:bg-gray-50">
                    📋 Copiar texto
                </button>
                <form asp-controller="Contratos" asp-action="Regerar" asp-route-id="@Model.Id" method="post">
                    @Html.AntiForgeryToken()
                    <button type="submit"
                            class="block w-full border border-blue-300 text-blue-600 px-4 py-3 rounded-lg font-medium text-center hover:bg-blue-50"
                            onclick="return confirm('Regerar contrato com IA? O texto atual será substituído.')">
                        ✨ Regerar com IA
                    </button>
                </form>
            </div>

            <div class="mt-4">
                <a asp-controller="Dashboard" asp-action="Index"
                   class="text-gray-500 text-sm hover:text-gray-700">← Voltar ao dashboard</a>
            </div>
        </aside>

        <!-- Conteúdo do contrato -->
        <main class="flex-1 bg-white p-10 rounded-xl border shadow-sm">
            <div id="contratoTexto" class="font-serif text-[15px] leading-7 whitespace-pre-wrap text-gray-800">@Model.Conteudo</div>
        </main>
    </div>
</div>

@section Scripts {
<script>
function copiarTexto() {
    const texto = document.getElementById('contratoTexto').innerText;
    navigator.clipboard.writeText(texto).then(() => alert('Texto copiado!'));
}
</script>
}
```

---

## 8. Program.cs

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/Login";
});

builder.Services.AddHttpClient<IIAService, IAService>();
builder.Services.AddScoped<IContratoService, ContratoService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Aplicar migrations automaticamente na inicialização
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
```

---

## 9. appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=SEU_HOST_NEON;Database=contratoia;Username=SEU_USER;Password=SUA_SENHA;SSL Mode=Require"
  },
  "Anthropic": {
    "ApiKey": "SUA_CHAVE_API_ANTHROPIC"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

> ⚠️ Nunca commitar appsettings.json com segredos reais. Use variáveis de ambiente no Render.

---

## 10. Pacotes NuGet necessários

Execute no terminal do projeto:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package QuestPDF
```

---

## 11. Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 12. Geração de PDF com QuestPDF

Implemente `GerarPdfAsync` no `ContratoService`:

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
```

---

## 13. Deploy no Render

Crie um `Dockerfile` na raiz:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ContratoIA.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContratoIA.dll"]
```

No Render:
- **Environment:** Docker
- **Variáveis de ambiente:** `ConnectionStrings__DefaultConnection`, `Anthropic__ApiKey`
- **Port:** 8080

---

## 14. Checklist de implementação

- [ ] Criar projeto: `dotnet new mvc -n ContratoIA`
- [ ] Instalar pacotes NuGet
- [ ] Criar enums, entities, DbContext
- [ ] Criar ViewModels
- [ ] Criar Services (IAService, ContratoService)
- [ ] Criar Controllers
- [ ] Criar Views (Layout, Header, Home, Account, Dashboard, Contratos)
- [ ] Configurar Program.cs
- [ ] Configurar appsettings.json (Neon + Anthropic)
- [ ] Rodar migrations
- [ ] Implementar GerarPdfAsync com QuestPDF
- [ ] Testar fluxo completo localmente
- [ ] Deploy no Render com variáveis de ambiente

---

_Prompt gerado para migração do ContratoIA (Lovable) → ASP.NET Core MVC_
_Data: 05/05/2026_