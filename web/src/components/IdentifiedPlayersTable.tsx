import { useMemo, useState } from 'react'
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
  // Por defecto, quien ha accedido más recientemente arriba; pulsando la cabecera se invierte. Quien nunca ha accedido, al final.
  const [newestFirst, setNewestFirst] = useState(true)
  const sorted = useMemo(() => {
    const time = (iso: string | null) => (iso ? new Date(iso).getTime() : null)
    return [...(links.data ?? [])].sort((a, b) => {
      const ta = time(a.lastSeenUtc), tb = time(b.lastSeenUtc)
      if (ta === null && tb === null) return a.id - b.id
      if (ta === null) return 1
      if (tb === null) return -1
      return (newestFirst ? tb - ta : ta - tb) || a.id - b.id
    })
  }, [links.data, newestFirst])

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
        subtitle="Dispositivos vinculados a un jugador, con la última vez que abrieron la app (se anota como mucho una vez por hora) y si fue como app instalada («App») o desde el navegador («Web»). Del dispositivo solo se guarda el hash de su identificador, nunca el identificador en claro. Si un jugador cambia de móvil, libera su dispositivo para que pueda volver a identificarse." />
      {(error ?? links.error) && <p className="error">{error ?? links.error}</p>}
      {!links.data && !links.error && <LoadingBlock />}
      {links.data && (
        links.data.length === 0 ? <p className="muted">Todavía no se ha identificado nadie.</p> : (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr><th>Id</th><th>Jugador</th><th>Id jugador</th><th>Vinculado</th>
                  <th aria-sort={newestFirst ? 'descending' : 'ascending'}>
                    <button type="button" className="sort-btn" onClick={() => setNewestFirst(v => !v)}
                      aria-label={`Último acceso, ordenado del ${newestFirst ? 'más reciente al más antiguo' : 'más antiguo al más reciente'}. Pulsa para invertir`}>
                      Último acceso <span aria-hidden="true">{newestFirst ? '▼' : '▲'}</span>
                    </button>
                  </th>
                  <th>Acceso</th>
                  <th></th></tr>
              </thead>
              <tbody>
                {sorted.map(l => (
                  <tr key={l.id}>
                    <td>{l.id}</td>
                    <td><PlayerName playerId={l.playerId}>{l.playerName}</PlayerName></td>
                    <td>{l.playerId}</td>
                    <td className="nowrap">{when(l.linkedAtUtc)}</td>
                    <td className="nowrap">{l.lastSeenUtc ? when(l.lastSeenUtc) : '—'}</td>
                    <td>{l.lastSeenAsApp === null ? '—' : l.lastSeenAsApp ? 'App' : 'Web'}</td>
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
