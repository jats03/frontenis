import { api } from '../api/client'
import { useDocumentTitle, useAsync } from '../hooks'
import LoadingBlock from './LoadingBlock'
import PageHeader from './PageHeader'
import PlayerName from './PlayerName'

const when = (iso: string) => new Date(iso).toLocaleString('es-ES', { dateStyle: 'short', timeStyle: 'medium', timeZone: 'Europe/Madrid' })

/** Administrador: todo lo que guarda la tabla de dispositivos vinculados (jugadores identificados). */
export default function IdentifiedPlayersTable() {
  useDocumentTitle('Jugadores identificados')
  const links = useAsync(() => api.deviceLinks.list(), [])

  return (
    <>
      <PageHeader title="Jugadores identificados"
        subtitle="Dispositivos vinculados a un jugador. Del dispositivo solo se guarda el hash de su identificador, nunca el identificador en claro." />
      {links.error && <p className="error">{links.error}</p>}
      {!links.data && !links.error && <LoadingBlock />}
      {links.data && (
        links.data.length === 0 ? <p className="muted">Todavía no se ha identificado nadie.</p> : (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr><th>Id</th><th>Jugador</th><th>Id jugador</th><th>Hash del dispositivo</th><th>Vinculado (Madrid)</th></tr>
              </thead>
              <tbody>
                {links.data.map(l => (
                  <tr key={l.id}>
                    <td>{l.id}</td>
                    <td><PlayerName playerId={l.playerId}>{l.playerName}</PlayerName></td>
                    <td>{l.playerId}</td>
                    <td className="device-hash"><code>{l.deviceHash}</code></td>
                    <td className="nowrap">{when(l.linkedAtUtc)}</td>
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
