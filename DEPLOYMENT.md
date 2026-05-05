# Guia de Deploy — ContratosIA no Render

> ASP.NET Core 10.0 MVC · EF Core · Identity · PostgreSQL · Docker

---

## Sumário

1. [Pré-deploy — Checklist](#1-pré-deploy--checklist)
2. [Configuração no Render Dashboard](#2-configuração-no-render-dashboard)
3. [Banco de Dados — PostgreSQL via Render](#3-banco-de-dados--postgresql-via-render)
4. [CORS e Configurações de Produção](#4-cors-e-configurações-de-produção)
5. [Troubleshooting — Erros Comuns](#5-troubleshooting--erros-comuns)

---

## 1. Pré-deploy — Checklist

### 1.1 Repositório no GitHub

1. O código **deve** estar em um repositório GitHub (público ou privado).
2. O Render precisa de acesso ao repositório — autorize via **Render Dashboard → Account → Connections**.
3. Confirme que a branch de deploy (`main`) está atualizada e testada localmente:

```bash
dotnet build
dotnet run   # teste em http://localhost:5000
```

### 1.2 Arquivos obrigatórios no repositório

| Arquivo | Status | Observação |
|---|---|---|
| `Dockerfile` | ✅ Já existe | Build multi-stage com SDK 10.0 |
| `render.yaml` | ✅ Já existe | Blueprint do Render |
| `.dockerignore` | ✅ Já existe | Exclui `bin/`, `obj/`, `.vs/` |
| `.gitignore` | ✅ Já existe | Exclui `appsettings.Development.json` |

### 1.3 Variáveis de ambiente obrigatórias

| Variável | Exemplo | Onde obter |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Fixo |
| `DATABASE_URL` | `postgres://user:pass@host:5432/dbname` | Render cria automaticamente ao vincular o banco |
| `OpenRouter__ApiKey` | `sk-or-v1-...` | Painel do OpenRouter |
| `OpenRouter__Model` | `deepseek/deepseek-v4-pro` | Fixo ou alterar conforme necessário |
| `OpenRouter__BaseUrl` | `https://openrouter.ai/api/v1` | Fixo |
| `PORT` | `8080` (default) | Render injeta automaticamente; o `Program.cs` já lê essa variável |

> **Nota sobre `ConnectionStrings__DefaultConnection`**: a variável `DATABASE_URL` tem prioridade no `Program.cs`. Se `DATABASE_URL` estiver presente, o app usa PostgreSQL; caso contrário, cai para SQLite. Em produção no Render, **use apenas `DATABASE_URL`**.

### 1.4 Comandos de build e start (referência Docker)

O `Dockerfile` já define tudo:

```dockerfile
# Build
RUN dotnet publish ContratosIA.csproj -c Release -o /app/publish --no-restore

# Start
ENTRYPOINT ["dotnet", "ContratosIA.dll"]
```

Se optar por **deploy nativo** (sem Docker), configure no Render:

| Campo | Valor |
|---|---|
| **Build Command** | `dotnet publish ContratosIA.csproj -c Release -o ./publish` |
| **Start Command** | `dotnet ./publish/ContratosIA.dll` |

---

## 2. Configuração no Render Dashboard

### 2.1 Tipo de serviço: **Web Service**

O ContratosIA é uma aplicação MVC server-rendered — use **Web Service** (não Static Site).

**Web Service** é o correto porque:
- Serve views Razor (CSHTML) processadas no servidor
- Usa Identity para autenticação via cookies
- Executa lógica de backend (EF Core, IA, geração de PDF)
- Precisa de um processo persistente escutando uma porta

**Static Site** serviria apenas se o app fosse HTML/CSS/JS puro sem backend.

### 2.2 Criando o serviço via Blueprint (recomendado)

O arquivo `render.yaml` já existe no repositório. Isso permite o deploy via **Blueprint**:

1. Acesse **[dashboard.render.com](https://dashboard.render.com)**.
2. Clique em **New** → **Blueprint**.
3. Selecione o repositório `ContratosIA`.
4. O Render lerá o `render.yaml` e criará automaticamente o Web Service.
5. Preencha as variáveis de ambiente que estão marcadas como `sync: false`:
   - `ConnectionStrings__DefaultConnection` (opcional — prefira `DATABASE_URL`)
   - `OpenRouter__ApiKey`

### 2.3 Criando o serviço manualmente

1. **New → Web Service**.
2. Conecte o repositório GitHub.
3. Configure:

| Campo | Valor |
|---|---|
| **Name** | `contratoia` |
| **Runtime** | **Docker** |
| **Region** | Oregon (ou o mais próximo dos seus usuários) |
| **Branch** | `main` |
| **Plan** | Free (ou Starter para zero cold starts) |
| **Dockerfile Path** | `./Dockerfile` |

### 2.4 Configuração do `render.yaml` atual

```yaml
services:
  - type: web
    name: contratoia
    runtime: docker
    plan: free
    region: oregon
    branch: main
    dockerfilePath: ./Dockerfile
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: ConnectionStrings__DefaultConnection
        sync: false
      - key: OpenRouter__ApiKey
        sync: false
      - key: OpenRouter__Model
        value: deepseek/deepseek-v4-pro
      - key: OpenRouter__BaseUrl
        value: https://openrouter.ai/api/v1
    healthCheckPath: /
    autoDeploy: true
```

> **Melhoria sugerida**: adicione a variável `DATABASE_URL` como `sync: false` para conectar ao banco PostgreSQL do Render (ver seção 3).

---

## 3. Banco de Dados — PostgreSQL via Render

### 3.1 Criando o banco no Render

1. No Dashboard, clique em **New → PostgreSQL**.
2. Configure:

| Campo | Valor sugerido |
|---|---|
| **Name** | `contratoia-db` |
| **Database** | `contratoia` |
| **User** | `contratoia` (gerado automaticamente) |
| **Region** | Mesma região do Web Service (Oregon) |
| **Plan** | Free (expira em 90 dias) ou Starter |

3. Após criado, o Render fornece uma **Internal Database URL** no formato:
   ```
   postgres://contratoia:abcd1234@dpg-xxx.oregon.render.com/contratoia
   ```

### 3.2 Conectando o banco ao Web Service

**Opção A — Link automático (recomendado)**:

1. Vá em **Web Service → Environment**.
2. Clique em **Add Environment Variable**.
3. Key: `DATABASE_URL`, Value: clique em **From Database** e selecione o PostgreSQL criado.
4. Salve — o serviço fará redeploy automaticamente.

**Opção B — Manual**:

Copie a **Internal Database URL** do banco e cole como valor da variável `DATABASE_URL` no Web Service.

### 3.3 Como o app detecta o banco

O `Program.cs` já implementa a lógica de seleção:

```csharp
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Usa PostgreSQL (produção no Render)
    var connStr = ParsePostgresUrl(databaseUrl);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connStr));
}
else
{
    // Usa SQLite (desenvolvimento local)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(...));
}
```

A função `ParsePostgresUrl` converte `postgres://user:pass@host:port/db` para o formato de connection string do Npgsql:

```
Host=...;Port=5432;Database=contratoia;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
```

### 3.4 Migrations

O app executa `db.Database.EnsureCreated()` automaticamente ao iniciar (quando `DATABASE_URL` está presente). Isso cria o schema na primeira execução.

> **Atenção**: `EnsureCreated()` ignora Migrations. Se você adicionar migrations no futuro e quiser aplicá-las incrementalmente, substitua por:
> ```csharp
> db.Database.Migrate();
> ```
> Isso requer que as migrations estejam compiladas no assembly publicado.

### 3.5 Atualizando o `render.yaml` para incluir o banco

```yaml
databases:
  - name: contratoia-db
    plan: free
    region: oregon
    databaseName: contratoia
    user: contratoia

services:
  - type: web
    name: contratoia
    runtime: docker
    plan: free
    region: oregon
    branch: main
    dockerfilePath: ./Dockerfile
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: DATABASE_URL
        fromDatabase:
          name: contratoia-db
          property: connectionString
      - key: OpenRouter__ApiKey
        sync: false
      - key: OpenRouter__Model
        value: deepseek/deepseek-v4-pro
      - key: OpenRouter__BaseUrl
        value: https://openrouter.ai/api/v1
    healthCheckPath: /
    autoDeploy: true
```

---

## 4. CORS e Configurações de Produção

### 4.1 CORS

O ContratosIA é uma app MVC server-rendered — as requisições vêm do próprio domínio (mesma origem). CORS **não é necessário** a menos que:

- Você adicione uma API REST que será consumida por um frontend em domínio diferente.
- Você chame endpoints externos via JavaScript do browser (neste caso, CORS é responsabilidade do servidor externo, não do ContratosIA).

Se no futuro precisar de CORS:

```csharp
// Program.cs — adicione antes de builder.Build()
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("https://seu-frontend.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Adicione antes de MapControllerRoute
app.UseCors("AllowSpecificOrigin");
```

### 4.2 HTTPS e HSTS

O `Program.cs` já configura:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
```

O Render termina TLS no load balancer e redireciona HTTP → HTTPS automaticamente. O app recebe tráfego na porta interna (8080) via HTTP, o que é correto.

### 4.3 Cookies de autenticação em produção

O Identity usa cookies por padrão. Em produção, os cookies devem ser:

- **SecureOnly = true** (já garantido pelo `UseHttpsRedirection` + HTTPS do Render).
- **SameSite = Lax** (default do ASP.NET Core, adequado para MVC).

Se precisar ajustar:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    // ...
});
```

### 4.4 Variáveis sensíveis

**Nunca** commite chaves de API no `appsettings.json`. O projeto já está correto:

- `appsettings.json` tem `OpenRouter__ApiKey` vazio.
- `appsettings.Development.json` está no `.gitignore`.
- No Render, todas as chaves são injetadas via **Environment Variables**.

O ASP.NET Core automaticamente lê variáveis de ambiente com `__` (duplo underscore) como hierarquia de configuração. Exemplo:

- Variável `OpenRouter__ApiKey` → lida como `OpenRouter:ApiKey` via `IConfiguration`.

### 4.5 Logging em produção

A configuração atual está adequada:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

Os logs aparecem em **Web Service → Logs** no Dashboard do Render.

---

## 5. Troubleshooting — Erros Comuns

### 5.1 Build Failure — SDK não encontrado

**Sintoma**: `error NETSDK1045: The current .NET SDK does not support .NET 10.0.`

**Causa**: O Dockerfile usa `mcr.microsoft.com/dotnet/sdk:10.0` que pode não estar disponível se a tag não existir.

**Solução**:

1. Verifique se a imagem base está disponível:
   ```bash
   docker pull mcr.microsoft.com/dotnet/sdk:10.0
   ```
2. Se o .NET 10 ainda estiver em preview, use a tag correta (ex.: `10.0-preview`).
3. Alternativamente, use o deploy nativo do Render (sem Docker) e especifique o .NET SDK version.

### 5.2 Build Failure — Restore falha (NuGet)

**Sintoma**: `Unable to resolve package QuestPDF` ou erro de restore.

**Causa**: O `dotnet restore` roda antes do `COPY . .`, então apenas o `.csproj` está disponível (o que é correto). Se o pacote não for encontrado, pode ser problema de cache ou versão.

**Solução**:

1. Teste localmente: `dotnet restore` e `dotnet build`.
2. Limpe o cache de build no Render: **Manual Deploy → Clear build cache**.
3. Verifique se a versão do QuestPDF (`2025.5.0`) existe no NuGet.

### 5.3 Port Binding Error

**Sintoma**: `Render detected that the service did not bind to the expected port within 60 seconds.` ou `EADDRINUSE`.

**Causa**: O Render injeta a variável `PORT` e espera que o app escute nela. O `Program.cs` já lida com isso:

```csharp
if (!app.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Urls.Add($"http://0.0.0.0:{port}");
}
```

**Verifique**:

1. A variável `ASPNETCORE_ENVIRONMENT` está como `Production` (se estiver `Development`, o código de bind de porta não executa).
2. O Dockerfile **não** define `ASPNETCORE_URLS` que poderia conflitar com a lógica do `Program.cs`.
3. O app binda em `0.0.0.0` (não `localhost` ou `127.0.0.1` — estes não são acessíveis externamente no container).

### 5.4 Database Connection Failed

**Sintoma**: `NpgsqlException: Failed to connect to [host]` ou `42P01: relation does not exist`.

**Diagnóstico**:

1. Verifique se `DATABASE_URL` está configurada no Render.
2. Confirme que o banco PostgreSQL está no mesmo **data center** (region) que o Web Service.
3. Use a **Internal URL** (não a External URL) para conexão entre serviços no Render.

**Solução**:

- Se o erro for de SSL: a connection string já inclui `SSL Mode=Require`.
- Se for "relation does not exist": o `EnsureCreated()` pode ter falhado. Verifique os logs de startup.
- Teste a connection string manualmente:
  ```bash
  docker exec -it contratosia-db psql -U contratosia -d contratoia -c "\dt"
  ```

### 5.5 502 Bad Gateway

**Sintoma**: O deploy termina com sucesso, mas ao acessar a URL retorna 502.

**Causas comuns**:

1. **Health check falhou**: O Render tenta acessar `healthCheckPath: /`. Se a rota `/` retornar erro (ex.: banco indisponível), o health check falha.
2. **App crashou após startup**: Verifique os logs em **Web Service → Logs**.
3. **Timeout no startup**: O plano free tem limites. Se `EnsureCreated()` demorar muito, o Render pode matar o processo.

**Solução**:

1. Ajuste o health check path se necessário:
   ```yaml
   healthCheckPath: /Home/Index
   ```
2. Aumente o timeout (em planos pagos).
3. Verifique se o banco está acessível antes do deploy.

### 5.6 Cold Starts (plano Free)

**Sintoma**: Primeira requisição demora 30-60 segundos.

**Causa**: No plano Free, o serviço "adormece" após 15 minutos de inatividade e leva ~30s para acordar.

**Soluções**:

1. Faça upgrade para **Starter ($7/mês)** — sem cold starts.
2. Use um serviço de ping (como UptimeRobot ou cron-job.org) para manter o serviço acordado (pode violar termos de uso).
3. Aceite a latência para projetos de demo/hobby.

### 5.7 Deploy travado em "Build in Progress"

**Sintoma**: O build não avança por mais de 10 minutos.

**Causas**:

1. Cache de build corrompido.
2. Imagem Docker base muito grande para baixar.

**Solução**:

1. **Manual Deploy → Clear build cache → Trigger Deploy**.
2. Verifique se o `.dockerignore` está excluindo pastas desnecessárias (`bin/`, `obj/`, `Plans/`).

### 5.8 Erro de Migração — SQLite vs PostgreSQL

**Sintoma**: `SQLiteException` em produção.

**Causa**: `DATABASE_URL` não está configurada, fazendo o app cair para SQLite. SQLite **não funciona** no Render porque o filesystem é efêmero (arquivos são perdidos a cada deploy).

**Solução**: Sempre configure `DATABASE_URL` apontando para o PostgreSQL do Render.

---

## Checklist Final — Antes do Deploy

- [ ] Repositório GitHub atualizado na branch `main`
- [ ] `Dockerfile`, `render.yaml` e `.dockerignore` presentes no repositório
- [ ] Variável `OpenRouter__ApiKey` configurada no Render
- [ ] Banco PostgreSQL criado no Render (mesma região)
- [ ] Variável `DATABASE_URL` apontando para o PostgreSQL (Internal URL)
- [ ] `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] Nenhuma chave de API no código-fonte
- [ ] Build local com `docker build -t contratoia .` passa sem erros
- [ ] `appsettings.Development.json` no `.gitignore`
