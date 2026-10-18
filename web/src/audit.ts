import { BASE, tokenStore } from './api/client'
import { deviceToken, hasDeviceToken } from './device'

/**
 * Avisa a la API de que se ha abierto una pantalla, para el registro de actividad del servidor.
 * Va por su cuenta (sin el aviso «Cargando…») y si falla no pasa nada.
 */
export function logPageView(path: string): void {
  try {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    const token = tokenStore.get()
    if (token) headers.Authorization = `Bearer ${token}`
    if (hasDeviceToken()) headers['X-Device-Token'] = deviceToken()
    void fetch(`${BASE}/api/audit/page`, { method: 'POST', headers, body: JSON.stringify({ path }), keepalive: true }).catch(() => undefined)
  } catch { /* el registro nunca debe molestar */ }
}
