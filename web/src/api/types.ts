export type Position = 'Delantero' | 'Zaguero'
export type CompetitionType = 'League' | 'Tournament' | 'RotatingLeague'
export type CompetitionStatus = 'Draft' | 'GroupStage' | 'Playoffs' | 'Finished'
export type MatchPhase = 'Group' | 'Playoff'

export interface Player { id: number; name: string; position: Position; groupNumber?: number | null }
export interface PairDto {
  entryId: number; pairId: number; name: string
  player1: Player; player2: Player; groupId: number | null
}
export interface PairingPreview {
  pairs: { player1: Player; player2: Player }[]
  unpaired: Player | null
}
export interface CompetitionSummary {
  id: number; name: string; type: CompetitionType; status: CompetitionStatus
  pairCount: number; groupCount: number; pairsPerGroup: number
  pointsPerWin: number; pointsPerLoss: number; doubleRound: boolean
  qualifiersPerGroup: number; registeredPairs: number; createdAtUtc: string
  rounds: number; nextCompetitionId: number | null
  /** 1 o 2 sets por partido (con 2, un 3.º de desempate). */
  setsPerMatch: number
}
export interface Group { id: number; name: string }
export interface MatchDto {
  id: number; phase: MatchPhase; groupId: number | null; groupName: string | null
  round: number; order: number
  homeEntryId: number | null; homeName: string | null
  awayEntryId: number | null; awayName: string | null
  homeScore: number | null; awayScore: number | null
  isPlayed: boolean; winnerEntryId: number | null
  sets: SetScore[] | null; homeSets: number | null; awaySets: number | null
}
export interface SetScore { home: number; away: number }
export interface CompetitionDetail {
  summary: CompetitionSummary; groups: Group[]; pairs: PairDto[]; matches: MatchDto[]
}
export interface StandingRow {
  rank: number; entryId: number; pairName: string; played: number; won: number; lost: number
  pointsFor: number; pointsAgainst: number; pointsDiff: number; classificationPoints: number
  qualifies: boolean
  /** 1 = sube de grupo, -1 = baja, 0 = se mantiene (solo ligas rotatorias terminadas). */
  movement: number
}
export interface GroupStandings { groupId: number; groupName: string; rows: StandingRow[] }
export interface CreateCompetition {
  name: string; type: CompetitionType; pairCount: number; groupCount: number
  pairsPerGroup: number; pointsPerWin: number; pointsPerLoss: number
  doubleRound: boolean; qualifiersPerGroup: number; rounds: number; setsPerMatch: number
}

export interface PlayerHistoryRow {
  playerId: number; name: string; position: Position
  played: number; won: number; lost: number; winRate: number
  setsFor: number; setsAgainst: number; tantosFor: number; tantosAgainst: number; tantosDiff: number
  classificationPoints: number; competitions: number
}
export interface Relation {
  playerId: number; name: string; played: number; won: number; lost: number
  winRate: number; tantosFor: number; tantosAgainst: number
}
export interface RecentMatch {
  matchId: number; competitionId: number; competitionName: string
  partner: string; rivals: string; sets: SetScore[]; won: boolean; isTraining: boolean
}
export interface PlayerHistory {
  summary: PlayerHistoryRow; partners: Relation[]; rivals: Relation[]
  recent: RecentMatch[]
}
/** Origen de los partidos del histórico; sin ids = todos. */
export interface HistoryFilter { competitionIds: number[]; trainingIds: number[] }

export interface TrainingSummary {
  id: number; name: string; date: string; pointsPerWin: number; pointsPerLoss: number; matchCount: number
}
export interface TrainingMatch {
  id: number; order: number; homeName: string; awayName: string
  homePlayerIds: number[]; awayPlayerIds: number[]
  homeScore: number; awayScore: number; homeWon: boolean
}
export interface TrainingRow {
  rank: number; name: string; playerIds: number[]; played: number; won: number; lost: number
  pointsFor: number; pointsAgainst: number; pointsDiff: number; classificationPoints: number
}
export interface TrainingDetail {
  summary: TrainingSummary; matches: TrainingMatch[]
  pairStandings: TrainingRow[]; playerStandings: TrainingRow[]
}
export interface TrainingSuggestion {
  homePlayerIds: number[]; awayPlayerIds: number[]; homeName: string; awayName: string
  homeLevel: number; awayLevel: number; balance: number; reason: string
}
export interface NewTrainingMatch {
  home1Id: number; home2Id: number; away1Id: number; away2Id: number
  homeScore: number; awayScore: number
}

// ---------- Reservas de entrenamientos ----------
export type WeekDay = 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'
/** Pendiente de abrir, abierta, ya empezada o cancelada (lo calcula el servidor con la hora de Madrid). */
export type SlotStatus = 'Pending' | 'Open' | 'Closed' | 'Cancelled'
export interface SlotAttendee { playerId: number; name: string }
export interface Slot {
  id: number
  /** «2026-10-19» */
  date: string
  /** «18:00:00» */
  start: string
  end: string
  /** Hora local de Madrid a partir de la cual se puede apuntar, sin zona horaria. */
  opensAt: string
  status: SlotStatus
  /** Menos jugadores que el mínimo recomendado (solo con la franja abierta). */
  fewPlayers: boolean
  /** Se abrió antes de su hora habitual y el administrador puede volver a cerrarla. */
  canCloseSignUps: boolean
  minPlayers: number
  templateId: number | null
  attendeeCount: number
  attendees: SlotAttendee[]
}
export interface SlotTemplate { id: number; day: WeekDay; start: string; end: string; active: boolean }
export interface SaveSlotTemplate { day: WeekDay; start: string; end: string; active: boolean }
export interface SaveSlot { date: string; start: string; end: string; opensAt?: string | null }

// ---------- Dispositivo, vínculo y apadrinados ----------
export interface PlayerRef { id: number; name: string }
/** Jugador titular de este dispositivo y los jugadores a los que además puede apuntar. */
export interface DeviceMe { owner: PlayerRef; delegates: PlayerRef[] }
export interface DeviceLink { playerId: number; playerName: string; linkedAtUtc: string }
export interface PlayerDelegateRow { ownerId: number; ownerName: string; delegateId: number; delegateName: string }
