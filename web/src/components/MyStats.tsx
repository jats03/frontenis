import { api } from '../api/client'
import type { PlayerHistory, Relation } from '../api/types'
import { ballLabel } from '../balls'
import { parseUtc } from '../dates'
import { useAsync } from '../hooks'
import { CalendarIcon, TrophyIcon, UsersIcon } from './Icons'
import PlayerAvatar from './PlayerAvatar'

/** Mínimo de partidos juntos para que un compañero cuente como «afinidad». */
const MIN_TOGETHER = 2

/** El compañero con el que más se gana (a igualdad, con el que más se ha jugado); hace falta haber jugado varios juntos. */
function bestPartner(partners: Relation[]): Relation | null {
  const ok = partners.filter(p => p.played >= MIN_TOGETHER)
  if (ok.length === 0) return null
  return [...ok].sort((a, b) => b.winRate - a.winRate || b.played - a.played || a.name.localeCompare(b.name))[0]
}

/** Fecha del partido (la API manda un instante UTC): en hora de Madrid, para que un partido de noche no salga el día anterior. */
const shortDate = (iso: string) =>
  parseUtc(iso).toLocaleDateString('es-ES', { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'Europe/Madrid' })

/**
 * Resumen de las estadísticas de quien se ha identificado, bajo las competiciones de la portada: compañero con más afinidad,
 * victorias, porcentaje de victorias y fecha del último partido. Si aún no ha jugado ninguno, no sale nada.
 */
export default function MyStats({ playerId, name }: { playerId: number; name: string }) {
  const { data } = useAsync<PlayerHistory>(() => api.players.playerHistory(playerId, { competitionIds: [], trainingIds: [] }), [playerId])
  if (!data || data.summary.played === 0) return null
  const { summary, partners, recent } = data
  const partner = bestPartner(partners)
  const last = recent[0]
  const pct = Math.max(0, Math.min(100, summary.winRate))
  const R = 42, C = 2 * Math.PI * R
  return (
    <section aria-labelledby="h-mis-stats">
      <h2 id="h-mis-stats" className="section-title">Mis estadísticas</h2>
      <div className="card my-stats">
        <div className="ms-top">
          <div className="ms-ring" role="img" aria-label={`${pct}% de victorias`}>
            <svg viewBox="0 0 100 100" aria-hidden="true">
              <circle className="ms-track" cx="50" cy="50" r={R} />
              <circle className="ms-bar" cx="50" cy="50" r={R} strokeDasharray={C} strokeDashoffset={C * (1 - pct / 100)} />
            </svg>
            <div className="ms-ring-text"><strong>{pct}%</strong><span>victorias</span></div>
          </div>
          <div className="ms-wins">
            <TrophyIcon />
            <strong>{summary.won}</strong>
            <span>{summary.won === 1 ? 'victoria' : 'victorias'}</span>
          </div>
        </div>

        <div className="ms-row ms-partner">
          {partner ? <PlayerAvatar playerId={partner.playerId} size={46} /> : <span className="ms-ico"><UsersIcon /></span>}
          <div>
            <span className="ms-label">Más afinidad</span>
            <strong>{partner ? partner.name : 'Aún sin datos'}</strong>
            <span className="muted">{partner ? `${partner.winRate}% de victorias juntos · ${partner.played} partidos` : 'Hacen falta varios partidos con la misma pareja'}</span>
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
      </div>
    </section>
  )
}
