/** Rasgos de un muñeco: se eligen siempre igual para el mismo jugador (a partir de su identificador). */
export interface AvatarParts {
  skin: string
  hair: string
  shirt: string
  background: string
  hairStyle: 'short' | 'long' | 'bun' | 'curly' | 'cap' | 'headband'
  mouth: 'smile' | 'grin'
}

const SKINS = ['#fdeee0', '#fbe6d4', '#f9dfca']
const HAIRS = ['#2b1d14', '#5a3a22', '#a5672b', '#d9a441', '#c8473a', '#4a4a55']
const SHIRTS = ['#0f766e', '#e4572e', '#3b6fd4', '#f2a900', '#8e44ad', '#2e9e5b', '#d6336c']
const BACKGROUNDS = ['#cdeae6', '#fde3d6', '#dbe6fb', '#fdf0c8', '#eadcf5', '#d7f0e0', '#fbdbe6']
const STYLES: AvatarParts['hairStyle'][] = ['short', 'long', 'bun', 'curly', 'cap', 'headband']

/** Mezcla un entero para que identificadores consecutivos no den muñecos parecidos. */
function mix(n: number): number {
  let x = (n + 0x9e3779b9) >>> 0
  x = Math.imul(x ^ (x >>> 16), 0x85ebca6b) >>> 0
  x = Math.imul(x ^ (x >>> 13), 0xc2b2ae35) >>> 0
  return (x ^ (x >>> 16)) >>> 0
}

const pick = <T,>(list: readonly T[], seed: number, salt: number): T => list[mix(seed * 31 + salt) % list.length]

export function avatarParts(playerId: number): AvatarParts {
  const color = mix(playerId * 7 + 3) % SHIRTS.length // camiseta y fondo van a juego
  return {
    skin: pick(SKINS, playerId, 1),
    hair: pick(HAIRS, playerId, 2),
    shirt: SHIRTS[color],
    background: BACKGROUNDS[color],
    hairStyle: pick(STYLES, playerId, 4),
    mouth: pick(['smile', 'grin'] as const, playerId, 5),
  }
}

/** «Buenos días», «Buenas tardes» o «Buenas noches» según la hora local. */
export function greetingFor(date: Date): string {
  const h = date.getHours()
  if (h >= 6 && h < 14) return 'Buenos días'
  if (h >= 14 && h < 21) return 'Buenas tardes'
  return 'Buenas noches'
}
