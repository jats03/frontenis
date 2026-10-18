import { useEffect } from 'react'
import { BASE } from '../api/client'

const WAKE_NOTICE_MS = 2500
const POLL_MS = 5000
const MAX_TRIES = 18

/**
 * Despierta la API al abrir la web (el plan gratuito de Render la duerme por inactividad). Si tarda, muestra el
 * aviso general «Cargando…» (las raquetas) hasta que responde; no pinta nada por sí mismo.
 */
export default function ApiStatus() {
  useEffect(() => {
    let cancelled = false
    let announced = false
    const notice = setTimeout(() => {
      if (cancelled) return
      announced = true
      window.dispatchEvent(new Event('frontenis:request-start'))
    }, WAKE_NOTICE_MS)

    async function wake() {
      for (let i = 0; i < MAX_TRIES && !cancelled; i++) {
        try {
          const res = await fetch(`${BASE}/health`, { cache: 'no-store' })
          if (res.ok) break
        } catch { /* durmiendo o sin red: reintenta */ }
        await new Promise(r => setTimeout(r, POLL_MS))
      }
      clearTimeout(notice)
      if (announced) { announced = false; window.dispatchEvent(new Event('frontenis:request-end')) }
    }
    void wake()

    return () => {
      cancelled = true
      clearTimeout(notice)
      if (announced) window.dispatchEvent(new Event('frontenis:request-end'))
    }
  }, [])

  return null
}
