using Frontenis.Domain.Enums;

namespace Frontenis.Domain.Entities;

/// <summary>Registro de una acción que modifica datos (POST, PUT o DELETE): quién, qué, cuándo y con qué resultado.</summary>
public sealed class AuditEntry
{
    public const int MaxPath = 300;
    public const int MaxName = 100;
    public const int MaxDevice = 120;

    private AuditEntry() { ActorName = string.Empty; Method = string.Empty; Path = string.Empty; }

    public int Id { get; private set; }
    public DateTime AtUtc { get; private set; }
    public ActorKind Actor { get; private set; }
    public string ActorName { get; private set; }
    public int? PlayerId { get; private set; }
    public string Method { get; private set; }
    public string Path { get; private set; }
    public int Status { get; private set; }
    public int DurationMs { get; private set; }
    /// <summary>Dispositivo (navegador, sistema y huella corta), solo al vincular o desvincular un móvil con su jugador.</summary>
    public string? Device { get; private set; }

    /// <summary>Crea un registro; recorta los textos largos para que quepan en la tabla.</summary>
    public static AuditEntry Create(DateTime atUtc, ActorKind actor, string actorName, int? playerId,
        string method, string path, int status, int durationMs, string? device = null) => new()
    {
        AtUtc = atUtc, Actor = actor, PlayerId = playerId, Status = status, DurationMs = durationMs,
        ActorName = Cut(actorName, MaxName), Method = Cut(method, 10), Path = Cut(path, MaxPath),
        Device = device is null ? null : Cut(device, MaxDevice),
    };

    private static string Cut(string text, int max) => text.Length <= max ? text : text[..max];
}
