import type { PlayerHistoryRow } from './api/types'

export type CardTier = 'bronze' | 'silver' | 'gold'
export interface CardStat { key: string; label: string; title: string; value: number }
export interface PlayerCardData { overall: number | null; tier: CardTier; stats: CardStat[]; played: number }

const clamp = (n: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, n))
/** Lleva un valor 0‑1 a la escala de las cartas: de 40 a 99. */
const scale = (ratio: number) => Math.round(40 + 59 * clamp(ratio, 0, 1))

/** Tantos que se juegan por set como máximo habitual; sirve para escalar ataque y defensa. */
const TANTOS_PER_SET = 10
/** Partidos y competiciones a partir de los cuales la experiencia llega al máximo. */
const MATCHES_FOR_MAX = 40
const COMPETITIONS_FOR_MAX = 8

/**
 * Convierte el histórico de un jugador en una carta estilo videojuego de fútbol.
 * Todo sale de partidos reales: ganar partidos y sets, tantos a favor y en contra, y veteranía.
 */
export function cardFromHistory(h: PlayerHistoryRow): PlayerCardData {
  const sets = h.setsFor + h.setsAgainst
  const tantosPerSet = (n: number) => (sets === 0 ? 0 : n / sets)
  const raw = {
    gan: h.played === 0 ? 0 : h.won / h.played,
    set: sets === 0 ? 0 : h.setsFor / sets,
    atq: tantosPerSet(h.tantosFor) / TANTOS_PER_SET,
    def: 1 - tantosPerSet(h.tantosAgainst) / TANTOS_PER_SET,
    par: h.played / MATCHES_FOR_MAX,
    com: h.competitions / COMPETITIONS_FOR_MAX,
  }
  const stats: CardStat[] = [
    { key: 'gan', label: 'GAN', title: 'Victorias', value: scale(raw.gan) },
    { key: 'set', label: 'SET', title: 'Sets ganados', value: scale(raw.set) },
    { key: 'atq', label: 'ATQ', title: 'Tantos a favor', value: scale(raw.atq) },
    { key: 'def', label: 'DEF', title: 'Tantos en contra', value: scale(raw.def) },
    { key: 'par', label: 'PAR', title: 'Partidos jugados', value: scale(raw.par) },
    { key: 'com', label: 'COM', title: 'Competiciones', value: scale(raw.com) },
  ]
  if (h.played === 0) return { overall: null, tier: 'bronze', stats, played: 0 }
  const overall = Math.round(
    stats[0].value * 0.35 + stats[1].value * 0.2 + stats[2].value * 0.15 +
    stats[3].value * 0.15 + stats[4].value * 0.1 + stats[5].value * 0.05)
  return { overall, tier: overall >= 75 ? 'gold' : overall >= 60 ? 'silver' : 'bronze', stats, played: h.played }
}

export const positionCode = (p: 'Delantero' | 'Zaguero') => (p === 'Delantero' ? 'DEL' : 'ZAG')
