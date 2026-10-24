import { describe, expect, it } from 'vitest'
import type { PlayerHistoryRow } from './api/types'
import { cardFromHistory, positionCode } from './cardStats'

const row = (over: Partial<PlayerHistoryRow> = {}): PlayerHistoryRow => ({
  playerId: 1, name: 'Ana', position: 'Delantero', played: 0, won: 0, lost: 0, winRate: 0,
  setsFor: 0, setsAgainst: 0, tantosFor: 0, tantosAgainst: 0, tantosDiff: 0, classificationPoints: 0, competitions: 0,
  ...over,
})

describe('cardFromHistory', () => {
  it('sin partidos no hay media y la carta es de bronce', () => {
    const c = cardFromHistory(row())
    expect(c.overall).toBeNull()
    expect(c.tier).toBe('bronze')
    expect(c.played).toBe(0)
    expect(c.stats).toHaveLength(6)
  })

  it('un historial perfecto llega al máximo de la escala (99) y es de oro', () => {
    const c = cardFromHistory(row({ played: 40, won: 40, setsFor: 40, tantosFor: 400, competitions: 8 }))
    expect(c.stats.find(s => s.key === 'gan')?.value).toBe(99)
    expect(c.stats.find(s => s.key === 'def')?.value).toBe(99)
    expect(c.overall).toBe(99)
    expect(c.tier).toBe('gold')
  })

  it('un historial muy malo se queda en 40 y es de bronce', () => {
    const c = cardFromHistory(row({ played: 10, won: 0, setsFor: 0, setsAgainst: 10, tantosFor: 0, tantosAgainst: 200 }))
    expect(c.stats.find(s => s.key === 'gan')?.value).toBe(40)
    expect(c.stats.find(s => s.key === 'def')?.value).toBe(40)
    expect(c.tier).toBe('bronze')
  })

  it('un historial medio es de plata', () => {
    const mid = cardFromHistory(row({ played: 20, won: 10, setsFor: 10, setsAgainst: 10, tantosFor: 100, tantosAgainst: 100, competitions: 4 }))
    expect(mid.overall).toBeGreaterThanOrEqual(50)
    expect(mid.overall).toBeLessThanOrEqual(70)
    expect(mid.tier).toBe('silver')
  })

  it('los valores nunca salen de 40-99 aunque los datos sean extremos', () => {
    const c = cardFromHistory(row({ played: 1000, won: 1000, setsFor: 1000, tantosFor: 100000, tantosAgainst: 100000, competitions: 100 }))
    for (const s of c.stats) {
      expect(s.value).toBeGreaterThanOrEqual(40)
      expect(s.value).toBeLessThanOrEqual(99)
    }
  })
})

describe('positionCode', () => {
  it('abrevia la posición', () => {
    expect(positionCode('Delantero')).toBe('DEL')
    expect(positionCode('Zaguero')).toBe('ZAG')
  })
})
