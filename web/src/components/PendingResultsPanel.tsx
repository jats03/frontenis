import { useState } from 'react'
import { confirmAction, notify } from '../alerts'
import { api } from '../api/client'
import type { PendingResult } from '../api/types'

interface Props {
  pending: PendingResult[]
  onChanged: () => void
}

/** Resultados enviados por jugadores, pendientes de que el administrador los apruebe o rechace. */
export default function PendingResultsPanel({ pending, onChanged }: Props) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function run(action: () => Promise<unknown>, done: string) {
    setBusy(true); setError(null)
    try { await action(); notify(done); onChanged() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  async function reject(p: PendingResult) {
    const ok = await confirmAction('¿Rechazar este resultado?',
      `${p.playerName} podrá volver a enviarlo.`, 'Sí, rechazar', 'Cancelar')
    if (ok) await run(() => api.matchSubmissions.reject(p.id), 'Resultado rechazado')
  }

  return (
    <section className="card pending-results">
      <h2>Resultados pendientes de aprobar ({pending.length})</h2>
      {error && <p className="error">{error}</p>}
      <ul className="plain">
        {pending.map(p => (
          <li key={p.id}>
            <div><strong>{p.homeName ?? '—'}</strong> – <strong>{p.awayName ?? '—'}</strong></div>
            <div>{p.sets.map(s => `${s.home}-${s.away}`).join(', ')} <span className="muted">· enviado por {p.playerName}</span></div>
            <div className="row">
              <button disabled={busy} onClick={() => void run(() => api.matchSubmissions.approve(p.id), 'Resultado aprobado')}>Aprobar</button>
              <button className="secondary" disabled={busy} onClick={() => void reject(p)}>Rechazar</button>
            </div>
          </li>
        ))}
      </ul>
    </section>
  )
}
