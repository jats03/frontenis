namespace Frontenis.Application.Interfaces.Services;

/// <summary>Crea sola la sesión de entrenamiento vacía de los martes y jueves.</summary>
public interface ITrainingAutoCreator
{
    /// <summary>
    /// Si hoy (hora de Madrid) es martes o jueves, ya son las 9:00 y ese día no se había gestionado, crea la sesión vacía
    /// (con el nombre por defecto y 3 puntos por victoria / 0 por derrota). Devuelve <c>true</c> si la ha creado.
    /// Es seguro llamarlo todas las veces que haga falta: solo actúa una vez por día.
    /// </summary>
    Task<bool> EnsureTodayAsync(CancellationToken ct = default);
}
