import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import { useAsync } from '../hooks'
import PlayerName from './PlayerName'

/** Administrador: qué jugadores pueden ver el resumen de tallas de todos. */
export default function SizeViewersPanel() {
  const viewers = useAsync(() => api.clothingSizes.viewers(), [])
  const players = useAsync(() => api.players.list(), [])
  const [choice, setChoice] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function change(run: () => Promise<unknown>, done: string) {
    setError(null)
    try { await run(); notify(done); setChoice(''); await viewers.reload() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const allowed = new Set((viewers.data ?? []).map(v => v.id))
  return (
    <section className="card form" aria-labelledby="quien-ve">
      <h2 id="quien-ve">Quién puede ver el resumen</h2>
      <p className="muted">Los jugadores que elijas verán, desde su dispositivo, las tallas de todos. El resto solo ve las suyas y las de sus apadrinados.</p>
      {(error ?? viewers.error) && <p className="error" role="alert">{error ?? viewers.error}</p>}
      <ul className="plain viewers-list">
        {(viewers.data ?? []).map(v => (
          <li key={v.id} className="row between">
            <strong><PlayerName playerId={v.id}>{v.name}</PlayerName></strong>
            <button type="button" className="secondary danger" onClick={() => void change(() => api.clothingSizes.removeViewer(v.id), 'Acceso quitado')}>Quitar</button>
          </li>
        ))}
        {viewers.data?.length === 0 && <li className="muted">Nadie tiene acceso todavía.</li>}
      </ul>
      <div className="row align-end">
        <label className="grow">Dar acceso a
          <select value={choice} onChange={e => setChoice(e.target.value)}>
            <option value="">Elegir…</option>
            {(players.data ?? []).filter(p => !allowed.has(p.id)).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </label>
        <button type="button" disabled={!choice} onClick={() => void change(() => api.clothingSizes.addViewer(Number(choice)), 'Acceso concedido')}>Dar acceso</button>
      </div>
    </section>
  )
}
