/** Rasgos de un muñeco: se eligen siempre igual para el mismo jugador (a partir de su identificador). */
export interface AvatarParts {
  skin: string
  hair: string
  shirt: string
  background: string
  hairStyle: 'short' | 'long' | 'bun' | 'curly' | 'cap' | 'headband' | 'bald' | 'mohawk' | 'ponytail' | 'afro' | 'buzz'
  mouth: 'smile' | 'grin' | 'neutral'
  accessory: 'none' | 'glasses' | 'sunglasses' | 'beard' | 'mustache' | 'earring'
}

/** Los 36 avatares de siempre: no cambian, para que nadie vea otro distinto del que tenía. */
export const AVATAR_BASE = 36
/** Cuántos avatares se pueden elegir (los mismos que valida el servidor): los de siempre y 36 nuevos con más variedad. */
export const AVATAR_CHOICES = 72

const SKINS = ['#fdeee0', '#fbe6d4', '#f9dfca']
const HAIRS = ['#2b1d14', '#5a3a22', '#a5672b', '#d9a441', '#c8473a', '#4a4a55']
const SHIRTS = ['#0f766e', '#e4572e', '#3b6fd4', '#f2a900', '#8e44ad', '#2e9e5b', '#d6336c']
const BACKGROUNDS = ['#cdeae6', '#fde3d6', '#dbe6fb', '#fdf0c8', '#eadcf5', '#d7f0e0', '#fbdbe6']
const STYLES: AvatarParts['hairStyle'][] = ['short', 'long', 'bun', 'curly', 'cap', 'headband']

// Solo para los avatares nuevos (del 37 en adelante, elegidos a mano): más tonos de piel, colores de pelo, peinados y complementos.
const X_SKINS = ['#fdeee0', '#f9dfca', '#f1c9a5', '#d9a37a', '#b9794f', '#8d5a3a', '#5e3a24']
const X_HAIRS = ['#111111', '#2b1d14', '#5a3a22', '#a5672b', '#e1c16e', '#c8473a', '#b4b4be', '#7a4bd1', '#2f7fd8', '#e85d9a']
const X_SHIRTS = [...SHIRTS, '#1d3557', '#c1121f', '#2a9d8f', '#6d597a']
const X_BACKGROUNDS = [...BACKGROUNDS, '#d3dbe8', '#f6d3d6', '#cfe9e5', '#e3dcea']
const X_STYLES: AvatarParts['hairStyle'][] = ['short', 'long', 'bun', 'curly', 'cap', 'headband', 'bald', 'mohawk', 'ponytail', 'afro', 'buzz']
const X_ACCESSORIES: AvatarParts['accessory'][] = ['none', 'glasses', 'sunglasses', 'beard', 'mustache', 'earring']

/** Mezcla un entero para que identificadores consecutivos no den muñecos parecidos. */
function mix(n: number): number {
  let x = (n + 0x9e3779b9) >>> 0
  x = Math.imul(x ^ (x >>> 16), 0x85ebca6b) >>> 0
  x = Math.imul(x ^ (x >>> 13), 0xc2b2ae35) >>> 0
  return (x ^ (x >>> 16)) >>> 0
}

const pick = <T,>(list: readonly T[], seed: number, salt: number): T => list[mix(seed * 31 + salt) % list.length]

/**
 * Rasgos del muñeco. `extended` (solo para un avatar nuevo elegido a mano, del 37 en adelante) usa el abanico ampliado;
 * sin él sale el de siempre, así que el avatar por defecto de cada jugador y los 36 primeros no cambian.
 */
export function avatarParts(seed: number, extended = false): AvatarParts {
  if (extended) {
    const color = mix(seed * 7 + 3) % X_SHIRTS.length
    return {
      skin: pick(X_SKINS, seed, 1),
      hair: pick(X_HAIRS, seed, 2),
      shirt: X_SHIRTS[color],
      background: X_BACKGROUNDS[color],
      hairStyle: pick(X_STYLES, seed, 4),
      mouth: pick(['smile', 'grin', 'neutral'] as const, seed, 5),
      accessory: pick(X_ACCESSORIES, seed, 6),
    }
  }
  const color = mix(seed * 7 + 3) % SHIRTS.length // camiseta y fondo van a juego
  return {
    skin: pick(SKINS, seed, 1),
    hair: pick(HAIRS, seed, 2),
    shirt: SHIRTS[color],
    background: BACKGROUNDS[color],
    hairStyle: pick(STYLES, seed, 4),
    mouth: pick(['smile', 'grin'] as const, seed, 5),
    accessory: 'none',
  }
}

/** Hora (0-23) en Madrid, sea cual sea la zona horaria del dispositivo. */
function madridHour(date: Date): number {
  const h = Number(new Intl.DateTimeFormat('es-ES', { hour: '2-digit', hourCycle: 'h23', timeZone: 'Europe/Madrid' }).format(date))
  return Number.isFinite(h) ? h : date.getHours()
}

/** «Buenos días», «Buenas tardes» o «Buenas noches» según la hora de Madrid. */
export function greetingFor(date: Date): string {
  const h = madridHour(date)
  if (h >= 6 && h < 12) return 'Buenos días'
  if (h >= 12 && h < 22) return 'Buenas tardes'
  return 'Buenas noches'
}
