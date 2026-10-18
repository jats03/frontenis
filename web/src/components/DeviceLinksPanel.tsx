import { notify } from '../alerts'
import { api } from '../api/client'
import { useAsync } from '../hooks'

/** Administrador: jugadores con dispositivo vinculado; se pueden liberar (por ejemplo, si cambian de móvil). */
export default function DeviceLinksPanel({ onError }: { onError: (message: string | null) => void }) {
  const links = useAsync(() => api.deviceLinks.list(), [])

  async function release(playerId: number) {
    onError(null)
    try { await api.deviceLinks.release(playerId); notify('Dispositivo liberado'); await links.reload() }
    catch (e) { onError(e instanceof Error ? e.message : 'Error') }
  }

  return (
    <>
      <h2 className="section-title">Dispositivos vinculados</h2>
      <p className="muted">Cada jugador se vincula una vez desde su móvil. Si cambia de móvil, libéralo aquí para que pueda volver a elegirse.</p>
      {links.error && <p className="error">{links.error}</p>}
      {links.data?.length === 0 && <p className="muted">Todavía no se ha vinculado nadie.</p>}
      <ul className="plain">
        {links.data?.map(l => (
          <li key={l.playerId} className="card row between">
            <span><strong>{l.playerName}</strong> <span className="muted">· desde el {new Date(l.linkedAtUtc).toLocaleDateString('es-ES')}</span></span>
            <button type="button" className="secondary" onClick={() => void release(l.playerId)}>Liberar</button>
          </li>
        ))}
      </ul>
    </>
  )
}
