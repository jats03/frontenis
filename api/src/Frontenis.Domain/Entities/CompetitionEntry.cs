namespace Frontenis.Domain.Entities;

/// <summary>Inscripción de una pareja en una competición (y su grupo tras el sorteo).</summary>
public sealed class CompetitionEntry
{
    private CompetitionEntry() { Pair = null!; }

    public CompetitionEntry(Pair pair) { Pair = pair; }

    public int Id { get; private set; }
    public int CompetitionId { get; private set; }
    public int PairId { get; private set; }
    public Pair Pair { get; private set; }
    public int? GroupId { get; private set; }
    public Group? Group { get; private set; }

    internal void AssignToGroup(Group group) => Group = group;
}
