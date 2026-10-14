-- Esquema de referencia (la API lo crea automáticamente con EnsureCreated).
-- Úsalo si prefieres crear la base de datos manualmente.
IF DB_ID('Frontenis') IS NULL CREATE DATABASE Frontenis;
GO
USE Frontenis;
GO
CREATE TABLE Players (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Position TINYINT NOT NULL,           -- 1 = Delantero, 2 = Zaguero
    GroupNumber INT NULL                 -- grupo de jugadores (1, 2, 3…) para seleccionarlos juntos
);
CREATE TABLE Pairs (
    Id INT IDENTITY PRIMARY KEY,
    Player1Id INT NOT NULL REFERENCES Players(Id),
    Player2Id INT NOT NULL REFERENCES Players(Id),
    CONSTRAINT CK_Pairs_Distinct CHECK (Player1Id <> Player2Id)
);
CREATE TABLE Competitions (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Type TINYINT NOT NULL,               -- 1 = Liga, 2 = Torneo, 3 = Liga rotatoria
    Status TINYINT NOT NULL,             -- 1 Borrador, 2 Grupos, 3 Fase final, 4 Finalizada
    PairCount INT NOT NULL,
    GroupCount INT NOT NULL,
    PairsPerGroup INT NOT NULL,
    PointsPerWin INT NOT NULL,
    PointsPerLoss INT NOT NULL,
    DoubleRound BIT NOT NULL,
    QualifiersPerGroup INT NOT NULL,
    Rounds INT NOT NULL DEFAULT 0,       -- solo liga rotatoria
    SetsPerMatch INT NOT NULL DEFAULT 1, -- 1 o 2 sets por partido (con 2, un 3.º de desempate)
    NextCompetitionId INT NULL,          -- liga siguiente (ascensos/descensos)
    CreatedAtUtc DATETIME2 NOT NULL
);
CREATE TABLE Groups (
    Id INT IDENTITY PRIMARY KEY,
    CompetitionId INT NOT NULL REFERENCES Competitions(Id) ON DELETE CASCADE,
    Name NVARCHAR(50) NOT NULL
);
CREATE TABLE CompetitionEntries (
    Id INT IDENTITY PRIMARY KEY,
    CompetitionId INT NOT NULL REFERENCES Competitions(Id) ON DELETE CASCADE,
    PairId INT NOT NULL REFERENCES Pairs(Id),
    GroupId INT NULL REFERENCES Groups(Id),
    CONSTRAINT UQ_Entry UNIQUE (CompetitionId, PairId)
);
CREATE TABLE Matches (
    Id INT IDENTITY PRIMARY KEY,
    CompetitionId INT NOT NULL REFERENCES Competitions(Id) ON DELETE CASCADE,
    Phase TINYINT NOT NULL,              -- 1 = Grupos, 2 = Fase final
    GroupId INT NULL REFERENCES Groups(Id),
    Round INT NOT NULL,
    [Order] INT NOT NULL,
    HomeEntryId INT NULL REFERENCES CompetitionEntries(Id),
    AwayEntryId INT NULL REFERENCES CompetitionEntries(Id),
    HomeScore INT NULL,
    AwayScore INT NULL,
    NextMatchId INT NULL REFERENCES Matches(Id),
    NextSlot TINYINT NULL,               -- 1 = Local, 2 = Visitante
    -- Tantos totales (suma de sets) pueden empatar; el ganador se decide por sets.
    Set1Home INT NULL, Set1Away INT NULL,
    Set2Home INT NULL, Set2Away INT NULL,
    Set3Home INT NULL, Set3Away INT NULL
);
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(60) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(300) NOT NULL,
    Role NVARCHAR(30) NOT NULL
);

-- Entrenamientos: partidos sueltos "a X tantos" con parejas libres
CREATE TABLE TrainingSessions (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    SessionDate DATETIME2 NOT NULL,
    PointsPerWin INT NOT NULL,
    PointsPerLoss INT NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL
);
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
);
CREATE INDEX IX_TrainingMatches_SessionId_Order ON TrainingMatches(SessionId, [Order]);
