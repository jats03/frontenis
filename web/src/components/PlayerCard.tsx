import { useEffect, useRef, useState } from 'react'
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
  const [hint, setHint] = useState<string | null>(null)
  const diff = summary ? summary.tantosFor - summary.tantosAgainst : 0
  const counts: [string, string, string][] = summary ? [
    ['V', 'Victorias', String(summary.won)], ['D', 'Derrotas', String(summary.lost)], ['SET', 'Sets ganados', String(summary.setsFor)],
    ['DT', 'Diferencia de tantos (a favor menos en contra)', diff > 0 ? `+${diff}` : diff < 0 ? `−${-diff}` : '0'],
    ['PAR', 'Partidos jugados', String(summary.played)],
  ] : []
  const hinted = counts.find(([label]) => label === hint)

  return (
    <dialog ref={ref} className="card-dialog" aria-label={name ? `Carta de ${name}` : 'Carta del jugador'}
      onClose={onClose} onClick={() => ref.current?.close()}>
      {!summary || !card ? (
        error ? <p role="alert">{error}</p> : <LoadingBlock />
      ) : (
        <div className={`fut-card ${card.tier}`}>
          <div className="fut-top">
            {card.played > 0 && (
              <div className="fut-rating">
                <strong>{card.overall}</strong>
                <span title={summary.position}>{positionCode(summary.position)}</span>
              </div>
            )}
            <PlayerAvatar playerId={playerId} size={150} className="fut-avatar" />
          </div>
          <h2 className="fut-name">{summary.name}</h2>
          <p className="fut-position">{summary.position}</p>
          {card.played > 0 && <>
          <ul className="fut-stats" aria-label="Estadísticas">
            {counts.map(([label, title, value]) => (
              <li key={label}>
                {/* En el móvil no hay «pasar el ratón»: al tocar una sigla se explica debajo. */}
                <button type="button" className="fut-stat" aria-pressed={hint === label} title={title}
                  onClick={e => { e.stopPropagation(); setHint(h => (h === label ? null : label)) }}>
                  <strong>{value}</strong>
                  <span aria-hidden="true">{label}</span>
                  <span className="sr-only"> {title}</span>
                </button>
              </li>
            ))}
          </ul>
          <p className="fut-hint" aria-live="polite">
            {hinted ? <span><strong>{hinted[0]}</strong> · {hinted[1]}</span> : 'Toca una sigla para ver qué significa'}
          </p>
          </>}
        </div>
      )}
      {summary && <button type="button" className="secondary fut-close" onClick={() => ref.current?.close()}>Cerrar</button>}
    </dialog>
  )
}
