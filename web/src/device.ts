const KEY = 'frontenis.device'
let memoryToken: string | null = null

function newToken(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID()
  const bytes = crypto.getRandomValues(new Uint8Array(16))
  return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('')
}

/** ¿Este navegador ya tiene identificador? (no lo crea). Sin él, el dispositivo no puede estar vinculado. */
export function hasDeviceToken(): boolean {
  try { return !!localStorage.getItem(KEY) } catch { return memoryToken !== null }
}

/**
 * Identificador aleatorio de este dispositivo (se genera la primera vez y se conserva en el navegador).
 * Sin almacenamiento (modo privado), vale solo mientras la pestaña esté abierta.
 */
export function deviceToken(): string {
  try {
    let t = localStorage.getItem(KEY)
    if (!t) { t = newToken(); localStorage.setItem(KEY, t) }
    return t
  } catch {
    return (memoryToken ??= newToken())
  }
}

/** La app está abierta como app instalada (no desde una pestaña del navegador). */
export const isStandalone = (): boolean =>
  window.matchMedia?.('(display-mode: standalone)').matches || (navigator as Navigator & { standalone?: boolean }).standalone === true

/** Cabecera con el identificador del dispositivo (la API solo guarda su hash). */
export const deviceHeaders = (): Record<string, string> => ({ 'X-Device-Token': deviceToken() })
