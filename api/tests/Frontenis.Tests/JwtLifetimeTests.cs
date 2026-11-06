using Frontenis.Domain.Entities;
using Frontenis.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Frontenis.Tests;

/// <summary>Duración del token del administrador: 24 horas, tanto por defecto como en appsettings.json.</summary>
public class JwtLifetimeTests
{
    private const string Key = "clave-de-prueba-de-al-menos-32-caracteres-xxxxxxxx";

    [Fact]
    public void Token_por_defecto_caduca_a_las_24_horas()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions { Key = Key }));
        var antes = DateTime.UtcNow;

        var (_, caduca) = service.Create(new User("admin", "hash", "Admin"));

        Assert.InRange(caduca - antes, TimeSpan.FromHours(24) - TimeSpan.FromSeconds(5), TimeSpan.FromHours(24) + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Appsettings_define_1440_minutos()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var config = new ConfigurationBuilder().AddJsonFile(ruta, optional: false).Build();

        Assert.Equal(1440, config.GetSection("Jwt").Get<JwtOptions>()!.ExpiresMinutes);
    }
}
