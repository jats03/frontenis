import { useState } from 'react'
import { confirmAction, confirmDelete, notify } from '../alerts'
import { api } from '../api/client'
import type { PendingResult, RejectedResult } from '../api/types'
import BallTag from './BallTag'

interface Props {
  pending: PendingResult[]
  /** Resultados rechazados que se conservan aparte, por si se rechazaron sin querer. */
  rejected?: RejectedResult[]
  onChanged: () => void
}

/** Resultados enviados por jugadores: el administrador los aprueba o rechaza, y puede recuperar los rechazados. */
export default function PendingResultsPanel({ pending, rejected = [], onChanged }: Props) {
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
      `${p.homeName ?? '—'} – ${p.awayName ?? '—'}: ${p.sets.map(s => `${s.home}-${s.away}`).join(', ')}\n` +
      `${p.playerName} podrá volver a enviarlo. Se guardará una copia por si lo rechazas sin querer.`, 'Sí, rechazar', 'Cancelar')
    if (ok) await run(() => api.matchSubmissions.reject(p.id), 'Resultado rechazado (guardado en «Rechazados»)')
  }

  async function restore(r: RejectedResult) {
    const ok = await confirmAction('¿Recuperar este resultado?', 'Volverá a quedar pendiente de aprobación.', 'Sí, recuperar', 'Cancelar')
    if (ok) await run(() => api.matchSubmissions.restore(r.id), 'Resultado recuperado')
  }

  async function removeRejected(r: RejectedResult) {
    if (!(await confirmDelete('¿Eliminar este resultado rechazado?', 'Se borrará definitivamente y ya no se podrá recuperar.'))) return
    await run(() => api.matchSubmissions.deleteRejected(r.id), 'Resultado eliminado')
  }

  return (
    <section className="card pending-results">
      {error && <p className="error">{error}</p>}
      {pending.length > 0 && (
        <>
          <h2>Resultados pendientes de aprobar ({pending.length})</h2>
          <ul className="plain">
            {pending.map(p => (
              <li key={p.id}>
                <div><strong>{p.homeName ?? '—'}</strong> – <strong>{p.awayName ?? '—'}</strong></div>
                <div>{p.sets.map(s => `${s.home}-${s.away}`).join(', ')} <BallTag ball={p.ball} /> <span className="muted">· enviado por {p.playerName}</span></div>
                <div className="row">
                  <button disabled={busy} onClick={() => void run(() => api.matchSubmissions.approve(p.id), 'Resultado aprobado')}>Aprobar</button>
                  <button className="secondary" disabled={busy} onClick={() => void reject(p)}>Rechazar</button>
                </div>
              </li>
            ))}
          </ul>
        </>
      )}
      {rejected.length > 0 && (
        <details>
          <summary>Rechazados ({rejected.length})</summary>
          <ul className="plain">
            {rejected.map(r => (
              <li key={r.id}>
                <div><strong>{r.homeName ?? '—'}</strong> – <strong>{r.awayName ?? '—'}</strong></div>
                <div>{r.sets.map(s => `${s.home}-${s.away}`).join(', ')} <BallTag ball={r.ball} /> <span className="muted">· enviado por {r.playerName}</span></div>
                <div className="row">
                  <button className="secondary" disabled={busy} onClick={() => void restore(r)}>Recuperar</button>
                  <button className="secondary" disabled={busy} onClick={() => void removeRejected(r)}>Eliminar</button>
                </div>
              </li>
            ))}
          </ul>
        </details>
      )}
    </section>
  )
}
