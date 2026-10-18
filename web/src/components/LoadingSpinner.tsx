import { useEffect, useState } from 'react'
import RacketLoader from './RacketLoader'

const DELAY_MS = 150 // evita parpadeos en peticiones muy rápidas

/** Spinner global: visible mientras haya alguna petición a la API en curso. */
export default function LoadingSpinner() {
  const [pending, setPending] = useState(0)
  const [visible, setVisible] = useState(false)

  useEffect(() => {
    const start = () => setPending(n => n + 1)
    const end = () => setPending(n => Math.max(0, n - 1))
    window.addEventListener('frontenis:request-start', start)
    window.addEventListener('frontenis:request-end', end)
    return () => {
      window.removeEventListener('frontenis:request-start', start)
      window.removeEventListener('frontenis:request-end', end)
    }
  }, [])

  useEffect(() => {
    if (pending === 0) { const t = setTimeout(() => setVisible(false), 0); return () => clearTimeout(t) }
    const t = setTimeout(() => setVisible(true), DELAY_MS)
    return () => clearTimeout(t)
  }, [pending])

  if (!visible) return null
  return (
    <div className="spinner-overlay" role="status" aria-live="polite" aria-label="Cargando">
      <div className="loader-card">
        <RacketLoader />
        <span className="loader-text" aria-hidden="true">Cargando…</span>
      </div>
      <span className="sr-only">Cargando…</span>
    </div>
  )
}
