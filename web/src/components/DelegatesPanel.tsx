import { useState, type FormEvent } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import type { Player } from '../api/types'
import { useAsync } from '../hooks'

/** Administrador: qué jugador puede apuntar también a qué otros (por ejemplo, un padre con sus hijos). */
export default function DelegatesPanel({ players, onError }: { players: Player[]; onError: (message: string | null) => void }) {
  const delegates = useAsync(() => api.playerDelegates.list(), [])
  const [owner, setOwner] = useState('')
  const [target, setTarget] = useState('')

  async function add(e: FormEvent) {
    e.preventDefault()
    onError(null)
    try {
      await api.playerDelegates.add(Number(owner), Number(target))
      setTarget('')
      notify('Apadrinado añadido')
      await delegates.reload()
    } catch (err) { onError(err instanceof Error ? err.message : 'Error') }
  }

  async function remove(ownerId: number, delegateId: number) {
    onError(null)
    try { await api.playerDelegates.remove(ownerId, delegateId); notify('Apadrinado quitado'); await delegates.reload() }
    catch (err) { onError(err instanceof Error ? err.message : 'Error') }
  }

  return (
    <>
      <h2 className="section-title">Apadrinados</h2>
      <p className="muted">Un jugador vinculado puede apuntarse a sí mismo y, además, a los jugadores que le asignes aquí.</p>
      {delegates.error && <p className="error">{delegates.error}</p>}
      <ul className="plain">
        {delegates.data?.map(d => (
          <li key={`${d.ownerId}-${d.delegateId}`} className="card row between">
            <span><strong>{d.ownerName}</strong> puede apuntar a <strong>{d.delegateName}</strong></span>
            <button type="button" className="secondary danger" onClick={() => void remove(d.ownerId, d.delegateId)}>Quitar</button>
          </li>
        ))}
      </ul>
      <form className="card form" onSubmit={add}>
        <h3>Añadir apadrinado</h3>
        <div className="cols">
          <label>Titular
            <select value={owner} onChange={e => setOwner(e.target.value)} required>
              <option value="">Elegir…</option>
              {players.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
          <label>Puede apuntar a
            <select value={target} onChange={e => setTarget(e.target.value)} required>
              <option value="">Elegir…</option>
              {players.filter(p => String(p.id) !== owner).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
            </select>
          </label>
        </div>
        <div className="row"><button type="submit">Añadir</button></div>
      </form>
    </>
  )
}
