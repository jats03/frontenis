namespace Frontenis.Domain.Interfaces;

/// <summary>Reloj del club: devuelve la hora local de Madrid (sin zona horaria) para poder sustituirlo en las pruebas.</summary>
public interface IClock
{
    /// <summary>Fecha y hora actuales en Madrid.</summary>
    DateTime Now { get; }
}
