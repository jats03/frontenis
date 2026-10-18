using System.Text.Json.Serialization;
using Frontenis.Api.Auditing;
using Frontenis.Api.Auditing.Interfaces;
using Frontenis.Api.Extensions;
using Frontenis.Api.HostedServices;
using Frontenis.Api.Middleware;
using Frontenis.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Cuerpos de petición pequeños: esta API solo recibe JSON diminuto.
builder.WebHost.ConfigureKestrel(o => { o.Limits.MaxRequestBodySize = 256 * 1024; o.AddServerHeader = false; });

builder.Services.AddFrontenis(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddProxyForwarding();
builder.Services.AddRateLimiting();
builder.Services.AddFrontenisCors(builder.Configuration);
builder.Services.AddHostedService<DatabaseInitializationService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IActorResolver, ActorResolver>();
builder.Services.AddHostedService<AuditPurgeService>();

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseMiddleware<SecurityHeadersMiddleware>();

// CORS va antes que el resto para que también las respuestas 429/401 lleguen al navegador con sus cabeceras CORS.
app.UseCors();
// Registra cada petición (quién, qué y resultado); va antes de la gestión de errores para ver también los 500.
app.UseMiddleware<RequestAuditMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

// Ligero y sin base de datos: para monitores de disponibilidad y para despertar la API en Render.
app.MapMethods("/health", new[] { "GET", "HEAD" }, () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program { }
