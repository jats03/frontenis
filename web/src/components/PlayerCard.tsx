import { useEffect, useRef } from 'react'
import { api } from '../api/client'
import type { HistoryFilter } from '../api/types'
import { cardFromHistory, positionCode } from '../cardStats'
import { useAsync } from '../hooks'
import LoadingBlock from './LoadingBlock'
import PlayerAvatar from './PlayerAvatar'

const ALL: HistoryFilter = { competitionIds: [], trainingIds: [] }

interface Props { playerId: number; onClose: () => void }

/** Carta del jugador (estilo videojuego de fútbol) en una superposición: avatar grande, posición y estadísticas. */
export default function PlayerCard({ playerId, onClose }: Props) {
  const ref = useRef<HTMLDialogElement>(null)
  const { data, error } = useAsync(() => api.players.playerHistory(playerId, ALL), [playerId])

  useEffect(() => {
    const d = ref.current
    if (d && !d.open) d.showModal()
  }, [])

  const summary = data?.summary
  const card = summary ? cardFromHistory(summary) : null
  const name = summary?.name ?? ''
  const counts: [string, string, number][] = summary ? [
    ['V', 'Victorias', summary.won], ['D', 'Derrotas', summary.lost], ['SET', 'Sets ganados', summary.setsFor],
    ['TF', 'Tantos a favor', summary.tantosFor], ['TC', 'Tantos en contra', summary.tantosAgainst], ['PAR', 'Partidos jugados', summary.played],
  ] : []

  return (
    <dialog ref={ref} className="card-dialog" aria-label={name ? `Carta de ${name}` : 'Carta del jugador'}
      onClose={onClose} onClick={() => ref.current?.close()}>
      {!summary || !card ? (
        error ? <p role="alert">{error}</p> : <LoadingBlock />
      ) : (
        <div className={`fut-card ${card.tier}`}>
          <div className="fut-top">
            <div className="fut-rating">
              <strong>{card.overall ?? '–'}</strong>
              <span title={summary.position}>{positionCode(summary.position)}</span>
            </div>
            <PlayerAvatar playerId={playerId} size={150} className="fut-avatar" />
          </div>
          <h2 className="fut-name">{summary.name}</h2>
          <p className="fut-position">{summary.position}</p>
          {card.played === 0 && <p className="fut-empty">Aún sin partidos: la carta se irá llenando al jugar.</p>}
          <ul className="fut-stats" aria-label="Estadísticas">
            {counts.map(([label, title, value]) => (
              <li key={label} title={title}>
                <strong>{value}</strong>
                <span aria-hidden="true">{label}</span>
                <span className="sr-only"> {title}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
      {summary && <button type="button" className="secondary fut-close" onClick={() => ref.current?.close()}>Cerrar</button>}
    </dialog>
  )
}
