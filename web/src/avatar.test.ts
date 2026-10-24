import { describe, expect, it } from 'vitest'
import { avatarParts, greetingFor } from './avatar'

describe('avatarParts', () => {
  it('es determinista: el mismo jugador siempre tiene el mismo muñeco', () => {
    expect(avatarParts(42)).toEqual(avatarParts(42))
  })

  it('camiseta y fondo van a juego y los rasgos son válidos', () => {
    const backgroundByShirt = new Map<string, string>()
    for (let id = 1; id <= 200; id++) {
      const p = avatarParts(id)
      expect(p.hair).toMatch(/^#[0-9a-f]{6}$/)
      expect(['short', 'long', 'bun', 'curly', 'cap', 'headband']).toContain(p.hairStyle)
      expect(['smile', 'grin']).toContain(p.mouth)
      const previous = backgroundByShirt.get(p.shirt)
      if (previous) expect(previous).toBe(p.background)
      backgroundByShirt.set(p.shirt, p.background)
    }
  })

  it('ids consecutivos no dan todos el mismo muñeco', () => {
    const looks = new Set(Array.from({ length: 30 }, (_, i) => JSON.stringify(avatarParts(i + 1))))
    expect(looks.size).toBeGreaterThan(10)
  })
})

describe('greetingFor', () => {
  // Instantes UTC: en octubre Madrid va en UTC+2.
  it.each([
    ['2026-10-21T03:59:00Z', 'Buenas noches'], // 05:59 en Madrid
    ['2026-10-21T04:00:00Z', 'Buenos días'],   // 06:00
    ['2026-10-21T09:59:00Z', 'Buenos días'],   // 11:59
    ['2026-10-21T10:00:00Z', 'Buenas tardes'], // 12:00
    ['2026-10-21T19:59:00Z', 'Buenas tardes'], // 21:59
    ['2026-10-21T20:00:00Z', 'Buenas noches'], // 22:00
  ])('%s -> %s', (iso, expected) => {
    expect(greetingFor(new Date(iso))).toBe(expected)
  })
})
