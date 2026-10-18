namespace Frontenis.Domain.Enums;

/// <summary>Quién hace una petición: el administrador, un jugador cuyo dispositivo está vinculado o un visitante sin identificar.</summary>
public enum ActorKind : byte
{
    Visitor = 0,
    Player = 1,
    Admin = 2,
}
