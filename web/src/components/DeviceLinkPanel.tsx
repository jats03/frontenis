import { useState } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import { useAsync } from '../hooks'
import type { useDevice } from '../useDevice'

type Device = ReturnType<typeof useDevice>

/** Indica quién eres en este dispositivo (sin cuenta ni contraseña) o permite elegirlo la primera vez. */
export default function DeviceLinkPanel({ device }: { device: Device }) {
  const { me } = device
  if (me === undefined) return null
  return me ? <Linked device={device} /> : <Pick device={device} />
}

function Linked({ device }: { device: Device }) {
  const [error, setError] = useState<string | null>(null)
  const me = device.me!
  async function unlink() {
    setError(null)
    try { await device.unlink(); notify('Dispositivo desvinculado', 'info') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }
  return (
    <div className="row between device-bar">
      <span>Eres <strong>{me.owner.name}</strong>
        {me.delegates.length > 0 && <span className="muted"> · también puedes apuntar a {me.delegates.map(d => d.name).join(', ')}</span>}
      </span>
      <button type="button" className="secondary" onClick={() => void unlink()}>No soy yo</button>
      {error && <p className="error" role="alert">{error}</p>}
    </div>
  )
}

function Pick({ device }: { device: Device }) {
  const players = useAsync(() => api.device.availablePlayers(), [])
  const [playerId, setPlayerId] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function link() {
    setError(null)
    try { await device.link(Number(playerId)); notify('¡Listo! Ya puedes apuntarte') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  return (
    <section className="card form" aria-labelledby="quien-eres">
      <h2 id="quien-eres">¿Quién eres?</h2>
      <p className="muted">Elígete una sola vez y podrás apuntarte a los entrenos con un toque. Se guarda en este móvil, sin cuenta ni contraseña.</p>
      {players.error && <p className="error">{players.error}</p>}
      <div className="row align-end">
        <label className="grow">Mi nombre
          <select value={playerId} onChange={e => setPlayerId(e.target.value)}>
            <option value="">Elegir…</option>
            {players.data?.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>
        </label>
        <button type="button" disabled={!playerId} onClick={() => void link()}>Soy yo</button>
      </div>
      {error && <p className="error" role="alert">{error}</p>}
      <p className="muted">¿No sale tu nombre? Puede que ya esté vinculado a otro móvil: pídele al administrador que lo libere.</p>
    </section>
  )
}
