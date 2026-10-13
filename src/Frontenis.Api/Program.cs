using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Frontenis.Api.Middleware;
using Frontenis.Infrastructure;
using Frontenis.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Cuerpos de petición pequeños: esta API solo recibe JSON diminuto.
builder.WebHost.ConfigureKestrel(o => { o.Limits.MaxRequestBodySize = 256 * 1024; o.AddServerHeader = false; });

builder.Services.AddFrontenis(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32) throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
// La clave de ejemplo del repositorio es pública: con ella cualquiera podría fabricar un token de administrador.
if (!builder.Environment.IsDevelopment() && jwt.Key.StartsWith("CAMBIA-ESTA-CLAVE", StringComparison.Ordinal))
    throw new InvalidOperationException("Jwt:Key es la clave de ejemplo. Define una clave secreta propia (variable de entorno Jwt__Key, p. ej. 64 caracteres aleatorios).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, // evita ataques de confusión de algoritmo ("none", etc.)
        ClockSkew = TimeSpan.FromSeconds(30)
    });
builder.Services.AddAuthorization();

// Render pone un proxy delante: sin esto, todas las peticiones parecerían venir de la misma IP.
// ForwardLimit = 1 usa solo la IP que añadió el proxy de Render, no la que el cliente pueda falsear.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// Límites de peticiones por IP: general (frena abusos) y estricto para login / cambio de contraseña (frena la fuerza bruta).
string ClientKey(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c =>
        RateLimitPartition.GetFixedWindowLimiter(ClientKey(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    // Alta pública de partidos de entrenamiento (sin sesión): límite bajo por IP para frenar el spam.
    o.AddPolicy("public-write", c =>
        RateLimitPartition.GetFixedWindowLimiter("public-write:" + ClientKey(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy("login", c =>
        RateLimitPartition.GetFixedWindowLimiter("login:" + ClientKey(c), _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// La inicialización de la BD se hace en segundo plano y con reintentos: así la API empieza a
// responder (/health) enseguida aunque Azure SQL esté pausado y tarde en reanudarse.
app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
{
    const int maxAttempts = 8;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await app.Services.InitializeDatabaseAsync(builder.Configuration);
            app.Logger.LogInformation("Base de datos lista.");
            return;
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Base de datos no disponible todavía (intento {Attempt}/{Max}).", attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
    app.Logger.LogError("No se pudo inicializar la base de datos tras {Max} intentos.", maxAttempts);
}));

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) app.UseHsts();

// Cabeceras de seguridad en todas las respuestas; las de autenticación nunca se cachean.
var isDevelopment = app.Environment.IsDevelopment();
app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    if (!isDevelopment)
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'"; // (Swagger necesita scripts en desarrollo)
    if (ctx.Request.Path.StartsWithSegments("/api/auth")) h["Cache-Control"] = "no-store";
    await next();
});

// CORS va antes que el resto para que también las respuestas 429/401 lleguen al navegador con sus cabeceras CORS.
app.UseCors();
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
