using System.Text;
using System.Text.Json.Serialization;
using Frontenis.Api.Middleware;
using Frontenis.Infrastructure;
using Frontenis.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrontenis(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32) throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

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
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization();

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
// Ligero y sin base de datos: para monitores de disponibilidad y para despertar la API en Render.
app.MapMethods("/health", new[] { "GET", "HEAD" }, () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();
app.Run();

public partial class Program { }
