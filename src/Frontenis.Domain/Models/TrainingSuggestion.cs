namespace Frontenis.Domain.Models;

/// <summary>Propuesta de partido: dos parejas y qué tan igualadas están.</summary>
public sealed record TrainingSuggestion(
    int Home1, int Home2, int Away1, int Away2,
    double HomeLevel, double AwayLevel, int Balance, string Reason);
