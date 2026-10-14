namespace Frontenis.Infrastructure.Persistence;

/// <summary>
/// Scripts idempotentes que ponen al día una BD ya existente: <c>EnsureCreated</c> crea las tablas
/// de una BD nueva pero no modifica las de una existente. El orden importa.
/// </summary>
internal static class SchemaScripts
{
    private static readonly string[] MatchSetColumns = { "Set1Home", "Set1Away", "Set2Home", "Set2Away", "Set3Home", "Set3Away" };

    public static IReadOnlyList<string> All { get; } = Build();

    private static List<string> Build()
    {
        var scripts = new List<string>
        {
            // Liga rotatoria
            "IF COL_LENGTH('Competitions','Rounds') IS NULL ALTER TABLE Competitions ADD Rounds INT NOT NULL CONSTRAINT DF_Competitions_Rounds DEFAULT 0;",
            "IF COL_LENGTH('Players','GroupNumber') IS NULL ALTER TABLE Players ADD GroupNumber INT NULL;",
            "IF COL_LENGTH('Competitions','NextCompetitionId') IS NULL ALTER TABLE Competitions ADD NextCompetitionId INT NULL;",

            // Sets por partido
            "IF COL_LENGTH('Competitions','SetsPerMatch') IS NULL ALTER TABLE Competitions ADD SetsPerMatch INT NOT NULL CONSTRAINT DF_Competitions_SetsPerMatch DEFAULT 1;",
        };
        scripts.AddRange(MatchSetColumns.Select(col =>
            $"IF COL_LENGTH('Matches','{col}') IS NULL ALTER TABLE Matches ADD {col} INT NULL;"));

        // Entrenamientos (partidos sueltos)
        scripts.Add(@"
IF OBJECT_ID('TrainingSessions') IS NULL
CREATE TABLE TrainingSessions (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    SessionDate DATETIME2 NOT NULL,
    PointsPerWin INT NOT NULL,
    PointsPerLoss INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);");
        scripts.Add(@"
IF OBJECT_ID('TrainingMatches') IS NULL
CREATE TABLE TrainingMatches (
    Id INT IDENTITY PRIMARY KEY,
    SessionId INT NOT NULL REFERENCES TrainingSessions(Id) ON DELETE CASCADE,
    [Order] INT NOT NULL,
    Home1Id INT NOT NULL REFERENCES Players(Id),
    Home2Id INT NOT NULL REFERENCES Players(Id),
    Away1Id INT NOT NULL REFERENCES Players(Id),
    Away2Id INT NOT NULL REFERENCES Players(Id),
    TargetPoints INT NOT NULL,
    HomeScore INT NOT NULL,
    AwayScore INT NOT NULL,
    PlayedAtUtc DATETIME2 NOT NULL
);");
        scripts.Add(
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TrainingMatches_SessionId_Order') " +
            "CREATE INDEX IX_TrainingMatches_SessionId_Order ON TrainingMatches(SessionId, [Order]);");

        // Los tantos totales (suma de sets) pueden empatar: el ganador se decide por sets.
        scripts.Add("IF OBJECT_ID('CK_Matches_NoTie') IS NOT NULL ALTER TABLE Matches DROP CONSTRAINT CK_Matches_NoTie;");
        return scripts;
    }
}
