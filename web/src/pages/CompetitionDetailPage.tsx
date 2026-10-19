import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { confirmDelete, notify, promptText } from '../alerts'
import { api, responseCache } from '../api/client'
import type { CompetitionDetail, GroupStandings, Player } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import ActionMenu from '../components/ActionMenu'
import Bracket from '../components/Bracket'
import PageHeader from '../components/PageHeader'
import MatchCard from '../components/MatchCard'
import RegistrationPanel from '../components/RegistrationPanel'
import RotatingDrawPanel from '../components/RotatingDrawPanel'
import RotatingAddPlayer from '../components/RotatingAddPlayer'
import MatchRoundMover from '../components/MatchRoundMover'
import RemoveRoundButton from '../components/RemoveRoundButton'
import SwapRoundSelect from '../components/SwapRoundSelect'
import AddMatchForm from '../components/AddMatchForm'
import RemoveMatchButton from '../components/RemoveMatchButton'
import SubTabs from '../components/SubTabs'
import StandingsTable from '../components/StandingsTable'
import { statusLabel, typeLabel } from '../labels'
import { holdLoader, useDocumentTitle } from '../hooks'
import { exportStandingsPdf } from '../pdf'
import LoadingBlock from '../components/LoadingBlock'

type Tab = 'standings' | 'matches' | 'playoffs' | 'pairs'

/** El componente interno se recrea al cambiar de competición para no arrastrar estado ni copias locales ajenas. */
export default function CompetitionDetailPage() {
  const id = Number(useParams().id)
  return <CompetitionDetailView key={id} id={id} />
}

function CompetitionDetailView({ id }: { id: number }) {
  const navigate = useNavigate()
  const { isAdmin } = useAuth()
  const cachedDetail = responseCache.peek<CompetitionDetail>(`/api/competitions/${id}`)
  const [detail, setDetail] = useState<CompetitionDetail | null>(cachedDetail?.data ?? null)
  const [standings, setStandings] = useState<GroupStandings[]>(
    () => responseCache.peek<GroupStandings[]>(`/api/competitions/${id}/standings`)?.data ?? [])
  const [players, setPlayers] = useState<Player[]>([])
  const [tab, setTab] = useState<Tab>('standings')
  const [standingsSel, setStandingsSel] = useState('all')            // 'all' o id de grupo
  const [matchMode, setMatchMode] = useState<'groups' | 'rounds'>('groups')
  const [matchSel, setMatchSel] = useState('all')                    // 'all', id de grupo o nº de jornada
  const [error, setError] = useState<string | null>(null)

  const hadDetail = useRef(detail !== null)
  const load = useCallback(async () => {
    const showsLoader = !hadDetail.current
    const startedAt = Date.now()
    try {
      const d = await api.competitions.get(id)
      const st = d.summary.status === 'Draft' ? [] : await api.competitions.standings(id)
      if (showsLoader) await holdLoader(startedAt)
      setDetail(d)
      setStandings(st)
      setError(null)
      hadDetail.current = true
    } catch (e) {
      if (showsLoader) await holdLoader(startedAt)
      setError(e instanceof Error ? e.message : 'Error')
    }
  }, [id])

  useEffect(() => { void load() }, [load])
  useEffect(() => { if (isAdmin) api.players.list().then(setPlayers).catch(() => undefined) }, [isAdmin])

  useDocumentTitle(detail?.summary.name ?? 'Competición')
  if (error && !detail) return <p className="error">{error}</p>
  if (!detail) return <LoadingBlock />

  const { summary } = detail
  const isTournament = summary.type === 'Tournament'
  const isRotating = summary.type === 'RotatingLeague'
  const groupMatches = detail.matches.filter(m => m.phase === 'Group')
  const hasPlayoffs = detail.matches.some(m => m.phase === 'Playoff')
  const canGeneratePlayoffs = isAdmin && isTournament && summary.status === 'GroupStage' && !hasPlayoffs &&
    groupMatches.length > 0 && groupMatches.every(m => m.isPlayed)

  const canEditRounds = isAdmin && isRotating && summary.status === 'GroupStage'
  const canAddMatches = isAdmin && isRotating && summary.status !== 'Draft'

  async function generatePlayoffs() {
    try { await api.competitions.playoffs(id); notify('Fase final generada'); await load(); setTab('playoffs') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  async function createNext() {
    try {
      const next = await api.competitions.nextLeague(id)
      notify(`Liga «${next.name}» creada`)
      navigate(`/competiciones/${next.id}`)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  async function rename() {
    const name = await promptText('Editar nombre', summary.name, 'Nombre de la competición')
    if (name === null || name === summary.name) return
    try { await api.competitions.rename(id, name); notify('Nombre actualizado'); await load() }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  async function changeSets() {
    try {
      await api.competitions.setSetsPerMatch(id, summary.setsPerMatch === 2 ? 1 : 2)
      notify('Formato de partido actualizado')
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  async function remove() {
    if (!(await confirmDelete('¿Eliminar esta competición?', 'Se borrarán también sus grupos, partidos y resultados.'))) return
    try { await api.competitions.remove(id); notify('Competición eliminada'); navigate('/competiciones') }
    catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }

  const tabs: [Tab, string][] = [
    ['standings', 'Clasificación'], ['matches', 'Partidos'],
    ...(isTournament ? [['playoffs', 'Fase final'] as [Tab, string]] : []),
    ...(isRotating ? [] : [['pairs', 'Parejas'] as [Tab, string]]),
  ]

  return (
    <>
      <PageHeader title={summary.name}
        badge={<span className={`badge ${summary.status}`}>{statusLabel[summary.status]}</span>}
        subtitle={<>
          {typeLabel[summary.type]} · {summary.pointsPerWin} pts por victoria
          {summary.pointsPerLoss > 0 && ` · ${summary.pointsPerLoss} por derrota`}
          {` · ${summary.setsPerMatch === 2 ? '2 sets (3.º de desempate)' : '1 set'} por partido`}
        </>}
        actions={
          <ActionMenu items={[
            { label: 'Exportar clasificación a PDF', hidden: standings.length === 0,
              onSelect: () => void exportStandingsPdf(summary.name, standings, isRotating) },
            { label: 'Editar nombre', hidden: !isAdmin, onSelect: () => void rename() },
            { label: `Pasar a ${summary.setsPerMatch === 2 ? '1 set' : '2 sets'} por partido`,
              hidden: !isAdmin || detail.matches.some(m => m.isPlayed), onSelect: () => void changeSets() },
            { label: 'Eliminar competición', danger: true, hidden: !isAdmin, onSelect: () => void remove() },
          ]} />
        } />
      {error && <p className="error">{error}</p>}

      {summary.status === 'Draft' && isAdmin && isRotating && <RotatingDrawPanel detail={detail} players={players}
        onChanged={load} onPlayersChanged={async () => { setPlayers(await api.players.list()); await load() }} />}
      {summary.status === 'Draft' && isAdmin && !isRotating && <RegistrationPanel detail={detail} players={players} onChanged={load}
        onPlayersChanged={async () => { setPlayers(await api.players.list()); await load() }} />}
      {summary.status === 'Draft' && !isAdmin && (isRotating
        ? <p className="muted">Pendiente de sorteo (mínimo {summary.pairCount} jugadores).</p>
        : <p className="muted">Inscripción abierta: {summary.registeredPairs}/{summary.pairCount} parejas.</p>)}

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

          {isRotating && summary.status === 'Finished' && (
            <div className="card row between">
              <span>
                Liga terminada: el primero de cada grupo sube (▲) y el último baja (▼).
              </span>
              {summary.nextCompetitionId
                ? <Link className="button" to={`/competiciones/${summary.nextCompetitionId}`}>Ver la siguiente liga</Link>
                : isAdmin && <button onClick={() => void createNext()}>Crear la siguiente liga</button>}
            </div>
          )}

          {tab === 'standings' && (
            <>
              {standings.length > 1 && (
                <SubTabs label="Grupos" value={standingsSel} onChange={setStandingsSel}
                  options={[{ key: 'all', label: 'Todos los grupos' }, ...standings.map(g => ({ key: String(g.groupId), label: g.groupName }))]} />
              )}
              {standings.filter(g => standingsSel === 'all' || String(g.groupId) === standingsSel).map(g => (
                <StandingsTable key={g.groupId} group={g} showQualifiers={isTournament} individual={isRotating} />
              ))}
            </>
          )}

          {tab === 'matches' && (
            <>
              <div className="view-switch">
                <span className="muted">Ver por:</span>
                <SubTabs label="Agrupar partidos" value={matchMode}
                  onChange={k => { setMatchMode(k as 'groups' | 'rounds'); setMatchSel('all') }}
                  options={[{ key: 'groups', label: 'Grupos' }, { key: 'rounds', label: 'Jornadas' }]} />
              </div>

              {matchMode === 'groups' && (
                <>
                  {detail.groups.length > 1 && (
                    <SubTabs label="Grupos" value={matchSel} onChange={setMatchSel}
                      options={[{ key: 'all', label: 'Todos' }, ...detail.groups.map(g => ({ key: String(g.id), label: g.name }))]} />
                  )}
                  {detail.groups.filter(g => matchSel === 'all' || String(g.id) === matchSel).map(g => {
                    const list = groupMatches.filter(m => m.groupId === g.id)
                    const rounds = [...new Set(list.map(m => m.round))].sort((a, b) => a - b)
                    return (
                      <section className="card" key={g.id}>
                        <h2>{g.name}</h2>
                        {rounds.map(r => (
                          <div key={r}>
                            <div className="round-head">
                              <h3>Jornada {r}</h3>
                              {canEditRounds && rounds.length > 1 && (
                                <div className="round-tools">
                                  <SwapRoundSelect competitionId={id} groupId={g.id} round={r} onChanged={load} onError={setError}
                                    others={rounds.filter(x => x !== r)} />
                                  {!list.some(m => m.round === r && m.isPlayed) && (
                                    <RemoveRoundButton competitionId={id} groupId={g.id} round={r} onChanged={load} onError={setError} />
                                  )}
                                </div>
                              )}
                            </div>
                            {list.filter(m => m.round === r).map(m => (
                              <div key={m.id}>
                                <MatchCard match={m} canEdit={isAdmin} setsPerMatch={summary.setsPerMatch} onChanged={() => void load()} />
                                {canEditRounds && !m.isPlayed && (
                                  <MatchRoundMover match={m} groupMatches={list} pairs={detail.pairs} onChanged={load} />
                                )}
                                {canAddMatches && list.length > 1 && (
                                  <div className="round-mover">
                                    <RemoveMatchButton matchId={m.id} played={m.isPlayed} onChanged={load} onError={setError} />
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        ))}
                        {canAddMatches && (
                          <AddMatchForm competitionId={id} groupId={g.id} rounds={rounds} pairs={detail.pairs} onChanged={load} />
                        )}
                      </section>
                    )
                  })}
                </>
              )}

              {matchMode === 'rounds' && (() => {
                const allRounds = [...new Set(groupMatches.map(m => m.round))].sort((a, b) => a - b)
                const shown = allRounds.filter(r => matchSel === 'all' || String(r) === matchSel)
                return (
                  <>
                    {allRounds.length > 1 && (
                      <SubTabs label="Jornadas" value={matchSel} onChange={setMatchSel}
                        options={[{ key: 'all', label: 'Todas' }, ...allRounds.map(r => ({ key: String(r), label: `Jornada ${r}` }))]} />
                    )}
                    {shown.map(r => (
                      <section className="card" key={r}>
                        <h2>Jornada {r}</h2>
                        {detail.groups.map(g => {
                          const list = groupMatches.filter(m => m.round === r && m.groupId === g.id)
                          if (list.length === 0) return null
                          return (
                            <div key={g.id}>
                              {detail.groups.length > 1 && <h3>{g.name}</h3>}
                              {list.map(m => (
                                <MatchCard key={m.id} match={m} canEdit={isAdmin} setsPerMatch={summary.setsPerMatch} onChanged={() => void load()} />
                              ))}
                            </div>
                          )
                        })}
                      </section>
                    ))}
                  </>
                )
              })()}
            </>
          )}

          {tab === 'matches' && canEditRounds && matchMode === 'groups' && (
            <RotatingAddPlayer detail={detail} players={players} onChanged={load} />
          )}

          {tab === 'playoffs' && <Bracket matches={detail.matches} canEdit={isAdmin} setsPerMatch={summary.setsPerMatch} onChanged={() => void load()} />}
        </>
      )}

      {!isRotating && (tab === 'pairs' || summary.status === 'Draft') && detail.pairs.length > 0 && (
        <section className="card">
          <h2>Parejas inscritas</h2>
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
