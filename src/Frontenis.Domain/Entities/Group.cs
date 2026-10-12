namespace Frontenis.Domain.Entities;

public sealed class Group
{
    private Group() { Name = string.Empty; }

    public Group(string name) { Name = name; }

    public int Id { get; private set; }
    public int CompetitionId { get; private set; }
    public string Name { get; private set; }
}
