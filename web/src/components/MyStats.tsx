import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { PlayerHistory } from '../api/types'
import { ballLabel } from '../balls'
import { parseUtc } from '../dates'
import { useAsync } from '../hooks'
import { CalendarIcon, TrophyIcon } from './Icons'

/** Fecha del partido (la API manda un instante UTC): en hora de Madrid, para que un partido de noche no salga el día anterior. */
const shortDate = (iso: string) =>
  parseUtc(iso).toLocaleDateString('es-ES', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'Europe/Madrid' })

/** Anillo con el % de victorias y, debajo, el número de victorias de ese periodo. */
function Ring({ title, row }: { title: string; row: { won: number; winRate: number } | undefined }) {
  const won = row?.won ?? 0
  const pct = Math.max(0, Math.min(100, row?.winRate ?? 0))
  const R = 42, C = 2 * Math.PI * R
  return (
    <div className="ms-block">
      <span className="ms-block-title">{title}</span>
      <div className="ms-ring" role="img" aria-label={`${title}: ${pct}% de victorias`}>
        <svg viewBox="0 0 100 100" aria-hidden="true">
          <circle className="ms-track" cx="50" cy="50" r={R} />
          <circle className="ms-bar" cx="50" cy="50" r={R} strokeDasharray={C} strokeDashoffset={C * (1 - pct / 100)} />
        </svg>
        <div className="ms-ring-text"><strong>{pct}%</strong><span>victorias</span></div>
      </div>
      <div className="ms-wins"><TrophyIcon /><strong>{won}</strong><span>{won === 1 ? 'victoria' : 'victorias'}</span></div>
    </div>
  )
}

/**
 * Resumen de las estadísticas de quien se ha identificado, en la portada: % de victorias y victorias (en total y en la semana y el mes actuales, a cero si no se jugó) y
 * el último partido. Si aún no ha jugado ninguno, no sale nada.
 */
export default function MyStats({ playerId, name }: { playerId: number; name: string }) {
  // Con la última copia guardada sale al instante y se refresca por detrás (la ruta es la clave de la copia).
  const { data } = useAsync<PlayerHistory>(() => api.players.playerHistory(playerId, { competitionIds: [], trainingIds: [] }), [playerId], `/api/players/${playerId}/history`)
  if (!data || data.summary.played === 0) return null
  const last = data.recent[0]
  return (
    <section aria-labelledby="h-mis-stats">
      <h2 id="h-mis-stats" className="section-title">Mis estadísticas</h2>
      <div className="card my-stats">
        <div className="ms-top">
          {(data.periods ?? []).map(p => <Ring key={p.kind} title={p.kind === 'week' ? 'Semana actual' : 'Mes actual'} row={p.stats} />)}
          <Ring title="Total" row={data.summary} />
        </div>

        {last && (
          <div className="ms-last">
            <div className="ms-last-head">
              <span className="ms-label">Último partido</span>
              <span className={`ms-result ${last.won ? 'won' : 'lost'}`}>{last.won ? 'Ganado' : 'Perdido'}</span>
            </div>
            <div className="ms-versus">
              <span className="ms-side"><strong>{name}{last.partner ? ` / ${last.partner}` : ''}</strong></span>
              <span className="ms-score">{last.sets.map((st, i) => <span key={i}>{st.home}–{st.away}</span>)}</span>
              <span className="ms-side"><strong>{last.rivals}</strong></span>
            </div>
            <div className="ms-meta">
              <span><CalendarIcon />{last.playedOn ? shortDate(last.playedOn) : 'Fecha desconocida'}</span>
              {last.ball && <span className="ms-ball">Bola {ballLabel(last.ball).toLowerCase()}</span>}
              <span>{last.isTraining ? last.competitionName : `Competición · ${last.competitionName}`}</span>
            </div>
          </div>
        )}

        <Link className="ms-more" to={`/jugadores/${playerId}`}>Ver más →</Link>
      </div>
    </section>
  )
}
