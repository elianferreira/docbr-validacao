using System.Threading.RateLimiting;
using Api.Host.Configuration;
using Api.Host.Modules.Documentos;
using Api.Host.Modules.Documentos.Api;
using Api.Host.Modules.Ocr;
using Api.Host.Modules.Ocr.Api;
using Api.Host.Modules.Ocr.Application;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Carrega .env em dev (no k8s a config vem por Secret/ConfigMap).
if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

// Nada de banner de stack: o header Server entrega o runtime de graca (§03 · fingerprint).
builder.WebHost.ConfigureKestrel(opcoes => opcoes.AddServerHeader = false);

builder.Services
    .AddOptions<OcrOptions>()
    .Bind(builder.Configuration.GetSection(OcrOptions.SectionName))
    .Validate(o => o.TamanhoMaximoBytes > 0, "Ocr__TamanhoMaximoBytes deve ser maior que zero.")
    .ValidateOnStart();

builder.Services
    .AddOptions<RateLimitOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName))
    .Validate(o => o.DocumentosPorMinuto > 0, "RateLimit__DocumentosPorMinuto deve ser maior que zero.")
    .Validate(o => o.OcrPorMinuto > 0, "RateLimit__OcrPorMinuto deve ser maior que zero.")
    .ValidateOnStart();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Este servico nao tem sessao: todo endpoint e anonimo. O GlobalLimiter do padrao particiona
// por identidade autenticada e deixaria o anonimo sem teto, entao a defesa aqui e uma policy
// por IP em cada grupo (§03 · ASVS 2.4.1).
builder.Services.AddRateLimiter(opcoes =>
{
    var limites = builder.Configuration
        .GetSection(RateLimitOptions.SectionName)
        .Get<RateLimitOptions>() ?? new RateLimitOptions();

    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opcoes.AddPolicy(RateLimitOptions.PolicyDocumentos, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            ChaveDoCliente(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limites.DocumentosPorMinuto,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opcoes.AddPolicy(RateLimitOptions.PolicyOcr, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            ChaveDoCliente(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limites.OcrPorMinuto,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddCors(opcoes =>
{
    // O front Expo roda em container separado; em dev ele sobe no 8081 do Metro.
    opcoes.AddPolicy("FrontendDev", politica => politica
        .WithOrigins("http://localhost:8081", "http://127.0.0.1:8081")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddDocumentosModule();
builder.Services.AddOcrModule();

var app = builder.Build();

// Erro nunca vaza nome de tabela, SQL ou stack trace para o cliente: o corpo leva o traceId
// e o diagnostico fica no log (§03 · fingerprint, §04 · Erros).
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // So em dev: o contrato alimenta o Orval do front, e em producao seria fingerprint.
    app.MapOpenApi();
}

app.UseCors("FrontendDev");
app.UseRateLimiter();

// Raiz anonima e sem banner de stack — e nela que as probes do k8s batem, nunca no /health
// (que depende do motor de OCR e derrubaria o pod).
app.MapGet("/", () => TypedResults.Ok(new { service = "docbr-validacao", status = "running" }))
    .AllowAnonymous()
    .RequireRateLimiting(RateLimitOptions.PolicyDocumentos)
    .WithName("GetRoot")
    .ExcludeFromDescription();

// Health checa a dependencia externa do servico. Aqui nao ha Oracle: o que pode faltar e o
// modelo de idioma do Tesseract.
app.MapGet("/health", (OcrService ocr) => ocr.MotorDisponivel
        ? Results.Ok(new { status = "ok", ocr = "carregado" })
        : Results.Problem(
            title: "Dependencia indisponivel",
            detail: "O motor de OCR nao esta carregado.",
            statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .RequireRateLimiting(RateLimitOptions.PolicyDocumentos)
    .WithName("GetHealth")
    .ExcludeFromDescription();

app.MapDocumentosEndpoints();
app.MapOcrEndpoints();

app.Run();

// Particiona o rate limit pelo IP de origem. Requisicao sem IP conhecido (proxy mal
// configurado, teste em memoria) cai toda numa particao unica, que e o lado seguro.
static string ChaveDoCliente(HttpContext contexto) =>
    contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

/// <summary>Exposto para os testes de integracao (<c>WebApplicationFactory&lt;Program&gt;</c>).</summary>
public partial class Program;
