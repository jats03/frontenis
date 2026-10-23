import { useState } from 'react'
import { confirmAction, notify } from '../alerts'
import { api } from '../api/client'
import { useAsync } from '../hooks'
import PlayerAvatar from './PlayerAvatar'
import type { DeviceState } from '../useDevice'

type Device = DeviceState

/** Indica quién eres en este dispositivo (sin cuenta ni contraseña) o permite elegirlo la primera vez. */
export default function DeviceLinkPanel({ device }: { device: Device }) {
  const { me } = device
  if (me === undefined) return null
  return me ? <Linked device={device} /> : <Pick device={device} />
}

/** Una vez identificado no hay botón para dejar de serlo: solo el administrador libera el dispositivo (o el propio jugador, borrando los datos del navegador). */
function Linked({ device }: { device: Device }) {
  const me = device.me!
  return (
    <div className="row between device-bar">
      <span className="device-who">
        <PlayerAvatar playerId={me.owner.id} size={36} />
        <span>Eres <strong>{me.owner.name}</strong>
        {me.delegates.length > 0 && <span className="muted"> · también puedes apuntar a {me.delegates.map(d => d.name).join(', ')}</span>}
        </span>
      </span>
    </div>
  )
}

function Pick({ device }: { device: Device }) {
  const players = useAsync(() => api.device.players(), [])
  const [playerId, setPlayerId] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function link() {
    setError(null)
    const chosen = players.data?.find(p => p.id === Number(playerId))
    // Solo si el servidor indica que ese nombre ya estaba identificado se pide confirmar.
    if (chosen?.linked && !(await confirmAction(`¿Estás seguro de que eres ${chosen.name}?`, '', 'Sí, soy yo', 'No'))) return
    try { await device.link(Number(playerId), chosen?.linked ?? false); notify('¡Listo! Ya puedes apuntarte') }
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
    </section>
  )
}
