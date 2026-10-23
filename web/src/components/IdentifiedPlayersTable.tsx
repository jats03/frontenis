import { useState } from 'react'
import { confirmAction, notify } from '../alerts'
import { api } from '../api/client'
import { formatDateTimeMadrid } from '../dates'
import { useDocumentTitle, useAsync } from '../hooks'
import LoadingBlock from './LoadingBlock'
import PageHeader from './PageHeader'
import PlayerName from './PlayerName'

const when = formatDateTimeMadrid

/** Administrador: todo lo que guarda la tabla de dispositivos vinculados (jugadores identificados). */
export default function IdentifiedPlayersTable() {
  useDocumentTitle('Jugadores identificados')
  const links = useAsync(() => api.deviceLinks.list(), [])
  const [error, setError] = useState<string | null>(null)

  /** Libera el dispositivo (por ejemplo, si el jugador cambia de móvil) para que pueda volver a identificarse. */
  async function release(playerId: number, name: string) {
    const ok = await confirmAction('¿Liberar este dispositivo?', `${name} tendrá que volver a identificarse desde su móvil.`, 'Sí, liberar', 'Cancelar')
    if (!ok) return
    setError(null)
    try { await api.deviceLinks.release(playerId); notify('Dispositivo liberado'); await links.reload() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  return (
    <>
      <PageHeader title="Jugadores identificados"
        subtitle="Dispositivos vinculados a un jugador. Del dispositivo solo se guarda el hash de su identificador, nunca el identificador en claro. Si un jugador cambia de móvil, libera su dispositivo para que pueda volver a identificarse." />
      {(error ?? links.error) && <p className="error">{error ?? links.error}</p>}
      {!links.data && !links.error && <LoadingBlock />}
      {links.data && (
        links.data.length === 0 ? <p className="muted">Todavía no se ha identificado nadie.</p> : (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr><th>Id</th><th>Jugador</th><th>Id jugador</th><th>Hash del dispositivo</th><th>Vinculado (Madrid)</th><th></th></tr>
              </thead>
              <tbody>
                {links.data.map(l => (
                  <tr key={l.id}>
                    <td>{l.id}</td>
                    <td><PlayerName playerId={l.playerId}>{l.playerName}</PlayerName></td>
                    <td>{l.playerId}</td>
                    <td className="device-hash"><code>{l.deviceHash}</code></td>
                    <td className="nowrap">{when(l.linkedAtUtc)}</td>
                    <td><button type="button" className="secondary" onClick={() => void release(l.playerId, l.playerName)}>Liberar</button></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      )}
    </>
  )
}
