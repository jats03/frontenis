const KEY = 'frontenis.device'
let memoryToken: string | null = null

function newToken(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID()
  const bytes = crypto.getRandomValues(new Uint8Array(16))
  return Array.from(bytes, b => b.toString(16).padStart(2, '0')).join('')
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

/** Cabecera con el identificador del dispositivo (la API solo guarda su hash). */
export const deviceHeaders = (): Record<string, string> => ({ 'X-Device-Token': deviceToken() })
