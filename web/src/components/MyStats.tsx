import { useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { PlayerHistory } from '../api/types'
import { BALLS, ballLabel, type BallType } from '../balls'
import { parseUtc } from '../dates'
import { useAsync } from '../hooks'
import { CalendarIcon } from './Icons'

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
      <div className="ms-wins"><strong>{won}</strong><span>{won === 1 ? 'victoria' : 'victorias'}</span></div>
    </div>
  )
}

const BALL_KEY = 'frontenis.statsBall'
const readBall = (): BallType | null => {
  try { const v = localStorage.getItem(BALL_KEY); return BALLS.some(b => b.key === v) ? (v as BallType) : null } catch { return null }
}

/**
 * Resumen de las estadísticas de quien se ha identificado, en la portada: % de victorias y victorias (en total y en la
 * semana y el mes actuales, a cero si no se jugó) y el último partido. Un selector permite contar solo una bola
 * (por defecto, todas; la elección se recuerda). Si aún no ha jugado ninguno, no sale nada.
 */
export default function MyStats({ playerId, name }: { playerId: number; name: string }) {
  const [ball, setBall] = useState<BallType | null>(readBall)
  const choose = (b: BallType | null) => {
    setBall(b)
    try { if (b) localStorage.setItem(BALL_KEY, b); else localStorage.removeItem(BALL_KEY) } catch { /* sin almacenamiento */ }
  }
  // Con la última copia guardada sale al instante y se refresca por detrás (la ruta es la clave de la copia).
  const base = `/api/players/${playerId}/history`
  const { data } = useAsync<PlayerHistory>(() => api.players.playerHistory(playerId, { competitionIds: [], trainingIds: [] }), [playerId], base)
  const byBall = useAsync<PlayerHistory | null>(
    () => (ball ? api.players.playerHistory(playerId, { competitionIds: [], trainingIds: [], balls: [ball] }) : Promise.resolve(null)),
    [playerId, ball], ball ? `${base}?balls=${ball}` : undefined)
  if (!data || data.summary.played === 0) return null
  // Las cifras de los anillos son las de la bola elegida; el último partido es siempre el último de todos.
  const shown = ball && byBall.data ? byBall.data : data
  const updating = !!ball && (byBall.loading || !byBall.data)
  const last = data.recent[0]
  return (
    <section aria-labelledby="h-mis-stats">
      <h2 id="h-mis-stats" className="section-title">Mis estadísticas</h2>
      <div className="card my-stats">
        <div className="ms-hero">
        <div className="ms-balls" role="radiogroup" aria-label="Bola">
          {[null, ...BALLS.map(b => b.key)].map(b => (
            <button key={b ?? 'all'} type="button" role="radio" aria-checked={ball === b}
              className={`ms-chip${ball === b ? ' on' : ''}`} onClick={() => choose(b)}>
              {b ? ballLabel(b) : 'Todas'}
            </button>
          ))}
        </div>
        <div className={`ms-top${updating ? ' updating' : ''}`}>
          {(shown.periods ?? []).map(p => <Ring key={p.kind} title={p.kind === 'week' ? 'Semana actual' : 'Mes actual'} row={p.stats} />)}
          <Ring title="Total" row={shown.summary} />
        </div>
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
