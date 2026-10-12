import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { CompetitionDetail, GroupStandings, Player } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import Bracket from '../components/Bracket'
import MatchCard from '../components/MatchCard'
import RegistrationPanel from '../components/RegistrationPanel'
import StandingsTable from '../components/StandingsTable'
import { statusLabel, typeLabel } from '../labels'
import { exportStandingsPdf } from '../pdf'

type Tab = 'standings' | 'matches' | 'playoffs' | 'pairs'

export default function CompetitionDetailPage() {
  const id = Number(useParams().id)
  const navigate = useNavigate()
  const { isAdmin } = useAuth()
  const [detail, setDetail] = useState<CompetitionDetail | null>(null)
  const [standings, setStandings] = useState<GroupStandings[]>([])
  const [players, setPlayers] = useState<Player[]>([])
  const [tab, setTab] = useState<Tab>('standings')
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const d = await api.competitions.get(id)
      setDetail(d)
      setStandings(d.summary.status === 'Draft' ? [] : await api.competitions.standings(id))
      setError(null)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }, [id])

  useEffect(() => { void load() }, [load])
  useEffect(() => { if (isAdmin) api.players.list().then(setPlayers).catch(() => undefined) }, [isAdmin])

  if (error && !detail) return <p className="error">{error}</p>
  if (!detail) return <p className="muted">Cargando…</p>

  const { summary } = detail
  const isTournament = summary.type === 'Tournament'
  const groupMatches = detail.matches.filter(m => m.phase === 'Group')
  const hasPlayoffs = detail.matches.some(m => m.phase === 'Playoff')
  const canGeneratePlayoffs = isAdmin && isTournament && summary.status === 'GroupStage' && !hasPlayoffs &&
    groupMatches.length > 0 && groupMatches.every(m => m.isPlayed)

  async function generatePlayoffs() {
    try { await api.competitions.playoffs(id); await load(); setTab('playoffs') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  async function remove() {
    if (!confirm('¿Eliminar esta competición y todos sus datos?')) return
    try { await api.competitions.remove(id); navigate('/') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const tabs: [Tab, string][] = [
    ['standings', 'Clasificación'], ['matches', 'Partidos'],
    ...(isTournament ? [['playoffs', 'Fase final'] as [Tab, string]] : []),
    ['pairs', 'Parejas'],
  ]

  return (
    <>
      <div className="row between">
        <div>
          <h2>{summary.name}</h2>
          <p className="muted">
            {typeLabel[summary.type]} · {statusLabel[summary.status]} · {summary.pointsPerWin} pts por victoria
            {summary.pointsPerLoss > 0 && ` · ${summary.pointsPerLoss} por derrota`}
          </p>
        </div>
        {isAdmin && <button className="secondary" onClick={() => void remove()}>Eliminar</button>}
      </div>
      {error && <p className="error">{error}</p>}

      {summary.status === 'Draft' && isAdmin && <RegistrationPanel detail={detail} players={players} onChanged={load}
        onPlayersChanged={async () => { setPlayers(await api.players.list()); await load() }} />}
      {summary.status === 'Draft' && !isAdmin && <p className="muted">Inscripción abierta: {summary.registeredPairs}/{summary.pairCount} parejas.</p>}

      {summary.status !== 'Draft' && (
        <>
          <div className="tabs" role="tablist">
            {tabs.map(([key, label]) => (
              <button key={key} role="tab" aria-selected={tab === key} className={tab === key ? 'active' : ''}
                onClick={() => setTab(key)}>{label}</button>
            ))}
          </div>

          {canGeneratePlayoffs && (
            <div className="card row between">
              <span>Fase de grupos completada.</span>
              <button onClick={() => void generatePlayoffs()}>Generar fase final</button>
            </div>
          )}

          {tab === 'standings' && (
            <>
              <div className="row">
                <button className="secondary" disabled={standings.length === 0}
                  onClick={() => void exportStandingsPdf(summary.name, standings)}>
                  Exportar clasificación a PDF
                </button>
              </div>
              {standings.map(g => (
                <StandingsTable key={g.groupId} group={g} showQualifiers={isTournament} />
              ))}
            </>
          )}

          {tab === 'matches' && detail.groups.map(g => {
            const list = groupMatches.filter(m => m.groupId === g.id)
            const rounds = [...new Set(list.map(m => m.round))].sort((a, b) => a - b)
            return (
              <section className="card" key={g.id}>
                <h3>{g.name}</h3>
                {rounds.map(r => (
                  <div key={r}>
                    <h4>Jornada {r}</h4>
                    {list.filter(m => m.round === r).map(m => (
                      <MatchCard key={m.id} match={m} canEdit={isAdmin} onChanged={() => void load()} />
                    ))}
                  </div>
                ))}
              </section>
            )
          })}

          {tab === 'playoffs' && <Bracket matches={detail.matches} canEdit={isAdmin} onChanged={() => void load()} />}
        </>
      )}

      {(tab === 'pairs' || summary.status === 'Draft') && detail.pairs.length > 0 && (
        <section className="card">
          <h3>Parejas inscritas</h3>
          <ul className="plain">
            {detail.pairs.map(p => {
              const group = detail.groups.find(g => g.id === p.groupId)
              return <li key={p.entryId}>{p.name} {group && <span className="muted">· {group.name}</span>}</li>
            })}
          </ul>
        </section>
      )}
    </>
  )
}
