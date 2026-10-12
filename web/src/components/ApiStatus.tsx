import { useEffect, useState } from 'react'
import { BASE } from '../api/client'

const WAKE_NOTICE_MS = 2500
const POLL_MS = 5000
const MAX_TRIES = 18

/**
 * Despierta la API al abrir la web (el plan gratuito de Render la duerme por inactividad) y muestra
 * un aviso si tarda, o si cualquier petición se alarga.
 */
export default function ApiStatus() {
  const [warming, setWarming] = useState(false)
  const [slow, setSlow] = useState(0)

  useEffect(() => {
    let cancelled = false
    const notice = setTimeout(() => { if (!cancelled) setWarming(true) }, WAKE_NOTICE_MS)

    async function wake() {
      for (let i = 0; i < MAX_TRIES && !cancelled; i++) {
        try {
          const res = await fetch(`${BASE}/health`, { cache: 'no-store' })
          if (res.ok) break
        } catch { /* durmiendo o sin red: reintenta */ }
        await new Promise(r => setTimeout(r, POLL_MS))
      }
      if (!cancelled) { clearTimeout(notice); setWarming(false) }
    }
    void wake()

    const start = () => setSlow(n => n + 1)
    const end = () => setSlow(n => Math.max(0, n - 1))
    window.addEventListener('frontenis:slow-start', start)
    window.addEventListener('frontenis:slow-end', end)
    return () => {
      cancelled = true
      clearTimeout(notice)
      window.removeEventListener('frontenis:slow-start', start)
      window.removeEventListener('frontenis:slow-end', end)
    }
  }, [])

  if (!warming && slow === 0) return null
  return (
    <div className="banner" role="status">
      <span className="spinner" aria-hidden="true" />
      Despertando el servidor… puede tardar hasta un minuto la primera vez. Gracias por la paciencia.
    </div>
  )
}
