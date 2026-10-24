namespace Frontenis.Domain.Entities;

/// <summary>
/// Marca que el entreno automático de un día ya se gestionó (se creó o ya existía uno). Sirve para no volver a crearlo
/// si el administrador lo borra después ese mismo día.
/// </summary>
public sealed class AutoTrainingRun
{
    private AutoTrainingRun() { }

    public AutoTrainingRun(DateTime date) { Date = date.Date; }

    public DateTime Date { get; private set; }
}
