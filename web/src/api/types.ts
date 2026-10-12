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
}
export interface Group { id: number; name: string }
export interface MatchDto {
  id: number; phase: MatchPhase; groupId: number | null; groupName: string | null
  round: number; order: number
  homeEntryId: number | null; homeName: string | null
  awayEntryId: number | null; awayName: string | null
  homeScore: number | null; awayScore: number | null
  isPlayed: boolean; winnerEntryId: number | null
}
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
  doubleRound: boolean; qualifiersPerGroup: number; rounds: number
}
