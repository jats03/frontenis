/** Mismas reglas de nombre que el servidor (PlayerNameRules): sirven para avisar al momento; el servidor es quien decide. */

/** Nombre limpio: sin caracteres invisibles ni de control, cualquier espacio como un espacio normal, sin repetidos ni en los extremos. */
export const normalizeName = (name: string) =>
  name.normalize('NFC')
    .replace(/[\s\p{Z}]+/gu, ' ')
    .replace(/[\p{Cc}\p{Cf}\p{Co}\p{Cn}\p{Cs}]/gu, '')
    .replace(/ +/g, ' ')
    .trim()

/** Clave para comparar: sin mayúsculas, tildes, espacios, guiones ni signos; solo letras y números. */
export const nameKey = (name: string) =>
  normalizeName(name).normalize('NFKD').replace(/\p{Mn}/gu, '').toLowerCase().replace(/ß/g, 'ss').replace(/[^\p{L}\p{N}]/gu, '')

export const MAX_NAME_LENGTH = 40
export const RESERVE_NAME = 'Jugador reserva'

/**
 * Mensaje de error si el nombre no vale (vacío, demasiado largo, reservado o ya usado por otro jugador), o null si está libre.
 * `selfId` es el jugador que se renombra: no choca consigo mismo.
 */
export function nameProblem(name: string, others: { id: number; name: string }[], selfId?: number): string | null {
  const clean = normalizeName(name)
  if (!clean) return 'El nombre es obligatorio.'
  if (clean.length > MAX_NAME_LENGTH) return `El nombre no puede pasar de ${MAX_NAME_LENGTH} caracteres.`
  const key = nameKey(clean)
  if (!key) return 'El nombre debe contener letras o números.'
  if (key === nameKey(RESERVE_NAME)) return `El nombre «${RESERVE_NAME}» está reservado.`
  const clash = others.find(p => p.id !== selfId && nameKey(p.name) === key)
  if (clash) return `Ya existe un jugador llamado «${clash.name}» (no se distinguen espacios, mayúsculas, tildes ni guiones).`
  return null
}
