import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { confirmAction, notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, MatchDto, PendingResult, SetScore } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import LoadingBlock from '../components/LoadingBlock'
import StopwatchPanel from '../components/StopwatchPanel'
import StopwatchMini from '../components/StopwatchMini'
import TimerHeader from '../components/TimerHeader'
import FoldSection from '../components/FoldSection'
import SubTabs from '../components/SubTabs'
import MatchSetupCard from '../components/MatchSetupCard'
import ServeDraw from '../components/ServeDraw'
import {
  clearSetup, colorName, colorOf, loadSetup, matchProgress, MAX_GOAL, MIN_GOAL, saveSetup, serverOf,
  type MatchSetup, type Side,
} from '../matchSetup'
import { useDocumentTitle } from '../hooks'
import { TIMER_KEY } from '../timerStore'
import { useDevice } from '../useDevice'
import BallPicker from '../components/BallPicker'
import BallTag from '../components/BallTag'
import { DEFAULT_BALL, type BallType } from '../balls'

interface Pts { h: number; a: number }
const ZERO = (): Pts[] => [{ h: 0, a: 0 }, { h: 0, a: 0 }, { h: 0, a: 0 }]
const MAX_POINTS = 99

const draftKey = (matchId: number) => `frontenis.matchdraft.${matchId}`
function loadDraft(matchId: number): Pts[] | null {
  try {
    const d = JSON.parse(localStorage.getItem(draftKey(matchId)) ?? 'null') as Pts[] | null
    return Array.isArray(d) && d.length === 3 && d.every(p => Number.isInteger(p?.h) && Number.isInteger(p?.a)) ? d : null
  } catch { return null }
}
function saveDraft(matchId: number, pts: Pts[]) {
  try {
    if (pts.every(p => p.h === 0 && p.a === 0)) localStorage.removeItem(draftKey(matchId))
    else localStorage.setItem(draftKey(matchId), JSON.stringify(pts))
  } catch { /* sin almacenamiento */ }
}
function clearDraft(matchId: number) { try { localStorage.removeItem(draftKey(matchId)) } catch { /* sin almacenamiento */ } }

export default function MatchPlayPage() {
  const params = useParams()
  const competitionId = Number(params.id)
  const matchId = Number(params.matchId)
  const { isAdmin } = useAuth()
  const { me } = useDevice()
  useDocumentTitle('Partido')

  const [detail, setDetail] = useState<CompetitionDetail | null>(null)
  const [pending, setPending] = useState<PendingResult[]>([])
  const [error, setError] = useState<string | null>(null)
  const [pts, setPts] = useState<Pts[] | null>(null)
  const [busy, setBusy] = useState(false)
  const [ballChoice, setBallChoice] = useState<BallType | null>(null)
  // Color de cada pareja, saque, tanteo y sets terminados: se guardan en este dispositivo mientras se juega.
  const [setup, setSetup] = useState<MatchSetup>(() => loadSetup(matchId))
  // Referencias con lo último: varios toques seguidos (antes de que la pantalla se repinte) no deben pisarse entre sí.
  const setupRef = useRef(setup)
  const ptsRef = useRef<Pts[] | null>(null)
  const updateSetup = (next: MatchSetup) => { setupRef.current = next; setSetup(next); saveSetup(matchId, next) }
  const updatePts = (next: Pts[]) => { ptsRef.current = next; setPts(next); saveDraft(matchId, next) }
  // Secciones plegables: el cronómetro empieza plegado (su hora y su botón se ven en la cabecera) y colores y saque solo
  // se despliegan si aún falta algo por elegir.
  const [open, setOpen] = useState(() => {
    const s = loadSetup(matchId)
    return { timer: false, setup: !(s.homeColor && s.serveStart), board: true, result: true }
  })
  const toggle = (k: keyof typeof open) => setOpen(o => ({ ...o, [k]: !o[k] }))
  const [tab, setTab] = useState(0) // pestaña del set que se está jugando
  // La barra fija del cronómetro solo se ve cuando su sección ya no está en pantalla.
  const timerBox = useRef<HTMLDivElement>(null)
  const [timerOut, setTimerOut] = useState(false)

  const load = useCallback(async () => {
    try {
      const [d, p] = await Promise.all([api.competitions.get(competitionId), api.competitions.pendingResults(competitionId)])
      setDetail(d); setPending(p); setError(null)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
  }, [competitionId])
  useEffect(() => { void load() }, [load])

  const playerId = me?.owner.id ?? null
  const mine = pending.find(p => p.matchId === matchId) ?? null

  // Marcador de partida: el borrador del navegador, si lo hay; si no, lo ya enviado por este jugador; si no, a cero.
  useEffect(() => {
    if (!detail || pts !== null) return
    const draft = loadDraft(matchId)
    if (draft) { ptsRef.current = draft; setPts(draft); return }
    const base = ZERO()
    if (mine && mine.playerId === playerId) {
      mine.sets.forEach((s, i) => { if (i < 3) base[i] = { h: s.home, a: s.away } })
      // Los sets ya enviados estaban terminados: se dan por terminados aunque no llegaran al tanteo.
      const finished = [false, false, false] as MatchSetup['finished']
      mine.sets.forEach((_, i) => { if (i < 3) finished[i] = true })
      updateSetup({ ...setupRef.current, finished })
    }
    ptsRef.current = base
    setPts(base)
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detail, pts, mine, matchId, playerId])

  const match: MatchDto | undefined = detail?.matches.find(m => m.id === matchId)
  const timerShown = !!match && !match.isPlayed && !!me && !isAdmin
  useEffect(() => {
    const el = timerBox.current
    if (!el || typeof IntersectionObserver === 'undefined') return
    const io = new IntersectionObserver(([e]) => setTimerOut(!e.isIntersecting), { threshold: 0 })
    io.observe(el)
    return () => io.disconnect()
  }, [timerShown])

  const back = <p><Link to={`/competiciones/${competitionId}`}>← Volver a la competición</Link></p>

  if (error && !detail) return <p className="error">{error}</p>
  if (!detail || me === undefined) return <LoadingBlock />
  if (!match) return <>{back}<p className="error">No se encuentra este partido.</p></>

  const home = match.homeName ?? 'Por definir'
  const away = match.awayName ?? 'Por definir'
  const setsPerMatch = detail.summary.setsPerMatch

  if (isAdmin) return <>{back}<h1>{home} – {away}</h1><p className="muted">El administrador introduce los resultados desde la propia competición.</p></>
  if (me === null) return <>{back}<h1>{home} – {away}</h1><p>Para enviar resultados tienes que identificarte desde el <Link to="/">inicio</Link>.</p></>
  if (match.isPlayed) {
    return <>{back}<h1>{home} – {away}</h1>
      <section className="card"><p><strong>Resultado: </strong>{match.sets?.map(s => `${s.home}-${s.away}`).join(', ') ?? `${match.homeScore}-${match.awayScore}`} <BallTag ball={match.ball} /></p></section></>
  }
  if (match.homeEntryId === null || match.awayEntryId === null) {
    return <>{back}<h1>{home} – {away}</h1><p className="muted">Este partido todavía no tiene las dos parejas definidas.</p></>
  }
  if (mine && mine.playerId !== playerId) {
    return <>{back}<h1>{home} – {away}</h1>
      <section className="card">
        <p><strong>Resultado pendiente de aprobación</strong></p>
        <p>{mine.sets.map(s => `${s.home}-${s.away}`).join(', ')} <BallTag ball={mine.ball} /> · enviado por {mine.playerName}</p>
        <p className="muted">Solo quien lo envió puede modificarlo, hasta que el administrador lo apruebe o lo rechace.</p>
      </section></>
  }

  const board = pts ?? ZERO()
  // Bola elegida a mano; si no, la del resultado ya enviado o la preolímpica.
  const ball: BallType = ballChoice ?? mine?.ball ?? DEFAULT_BALL
  const progress = matchProgress(board, setup, setsPerMatch)
  const { visible } = progress
  const active = Math.min(tab, visible - 1)
  const nameOf = (side: Side) => (side === 'h' ? home : away)
  const setLabel = (i: number) => (visible === 1 ? 'Partido' : i === 2 ? 'Desempate' : `Set ${i + 1}`)

  /** Tras un cambio, si el set que se juega acaba de decidirse se pasa solo a la pestaña del siguiente. */
  function advanceIfDecided(nextPts: Pts[], nextSetup: MatchSetup) {
    const before = matchProgress(ptsRef.current ?? board, setupRef.current, setsPerMatch).results[active].winner
    const after = matchProgress(nextPts, nextSetup, setsPerMatch)
    if (before === null && after.results[active].winner !== null && active < after.visible - 1) setTab(active + 1)
  }

  function change(i: number, side: Side, delta: number) {
    const cur = ptsRef.current ?? board
    const curSetup = setupRef.current
    // Cerrado el set (alguien llegó al tanteo o se dio por terminado) no se suben más puntos; sí se pueden bajar para corregir.
    if (delta > 0 && matchProgress(cur, curSetup, setsPerMatch).results[i].winner !== null) return
    const next = cur.map((p, k) => (k === i ? { ...p, [side]: Math.min(MAX_POINTS, Math.max(0, p[side] + delta)) } : p))
    // Un set dado por terminado que vuelve a quedar empatado se reabre.
    let nextSetup = curSetup
    if (curSetup.finished[i] && next[i].h === next[i].a) {
      const finished = [...curSetup.finished] as MatchSetup['finished']; finished[i] = false
      nextSetup = { ...curSetup, finished }
    }
    advanceIfDecided(next, nextSetup)
    updatePts(next)
    if (nextSetup !== curSetup) updateSetup(nextSetup)
  }
  function changeGoal(i: number, delta: number) {
    const goals = [...setupRef.current.goals] as MatchSetup['goals']
    goals[i] = Math.min(MAX_GOAL, Math.max(MIN_GOAL, goals[i] + delta))
    const nextSetup = { ...setupRef.current, goals }
    advanceIfDecided(ptsRef.current ?? board, nextSetup)
    updateSetup(nextSetup)
  }
  async function finishSet(i: number) {
    const { h, a } = ptsRef.current?.[i] ?? board[i]
    if (h === a) { setError(`${setLabel(i)}: no puede acabar en empate. Sube un punto a una de las parejas.`); return }
    setError(null)
    const leader: Side = h > a ? 'h' : 'a'
    const ok = await confirmAction('¿Dar el set por terminado?',
      `${setLabel(i)}: ${h}-${a}.\nGana ${nameOf(leader)} por ir por delante al acabar el tiempo.`, 'Sí, terminar el set', 'Seguir jugando')
    if (!ok) return
    const finished = [...setupRef.current.finished] as MatchSetup['finished']; finished[i] = true
    const nextSetup = { ...setupRef.current, finished }
    advanceIfDecided(ptsRef.current ?? board, nextSetup)
    updateSetup(nextSetup)
  }
  function reopenSet(i: number) {
    const finished = [...setupRef.current.finished] as MatchSetup['finished']; finished[i] = false
    updateSetup({ ...setupRef.current, finished })
  }
  async function zero() {
    const ok = await confirmAction('¿Limpiar el marcador?', 'Se pondrán a cero todos los sets de este partido.', 'Sí, limpiar', 'Cancelar')
    if (!ok) return
    clearDraft(matchId); ptsRef.current = ZERO(); setPts(ptsRef.current); setError(null); setTab(0)
    updateSetup({ ...setupRef.current, finished: [false, false, false] })
  }

  async function send() {
    const sets: SetScore[] = board.slice(0, visible).map(p => ({ home: p.h, away: p.a }))
    setError(null)
    const resumen = sets.map((s, i) => `${visible > 1 ? setLabel(i) + ': ' : ''}${s.home}-${s.away}`).join(' · ')
    const ok = await confirmAction('¿Enviar el resultado?',
      `${home} – ${away}\n${resumen}\nQuedará pendiente de aprobación del administrador.`, 'Sí, enviar', 'Seguir jugando')
    if (!ok) return
    setBusy(true)
    try {
      await api.matches.submitResult(matchId, sets, ball)
      clearDraft(matchId); clearSetup(matchId)
      notify('Resultado enviado, pendiente de aprobación')
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  const sideTitle = (name: string, hasServe: boolean) => (
    <span className="board-team">{hasServe && <span className="serve-dot" role="img" aria-label="Saca" />}{name}</span>
  )
  const tag = (i: number) => (progress.results[i].winner ? ' ✓' : '')
  const colorText = (side: Side) => { const c = colorOf(setup, side); return c ? ` (${colorName(c)})` : '' }
  const serveText = setup.serveStart ? `saca ${nameOf(setup.serveStart)}` : 'saque sin sortear'
  const setupSummary = setup.homeColor ? `${home}${colorText('h')} · ${away}${colorText('a')} · ${serveText}` : `Sin colores · ${serveText}`
  const scoreSummary = Array.from({ length: visible }, (_, i) => `${board[i].h}-${board[i].a}`).join(' · ')
  const winnerName = progress.winner ? nameOf(progress.winner) : ''
  const timerKey = `${TIMER_KEY}.match.${matchId}`

  return (
    <>
      {back}
      <h1>{home} – {away}</h1>
      {mine && (
        <p className="notice">Resultado enviado ({mine.sets.map(s => `${s.home}-${s.away}`).join(', ')}): pendiente de aprobación del
          administrador. Puedes corregirlo y volver a enviarlo.</p>
      )}

      <div ref={timerBox}>
        <FoldSection title="Cronómetro" open={open.timer} onToggle={() => toggle('timer')} summary={<TimerHeader storageKey={timerKey} />}>
          <StopwatchPanel storageKey={timerKey} compact hideDisplay sound={false} />
        </FoldSection>
      </div>
      <StopwatchMini storageKey={timerKey} visible={timerOut} />

      <FoldSection title="Colores y saque" open={open.setup} onToggle={() => toggle('setup')}
        summary={!open.setup ? <span className="fold-text">{setupSummary}</span> : undefined}>
        <MatchSetupCard home={home} away={away} setup={setup} onChange={updateSetup} />
      </FoldSection>

      <FoldSection title="Marcador" open={open.board} onToggle={() => toggle('board')}
        summary={<span className="fold-text">{scoreSummary}</span>}>
        <SubTabs label="Sets" value={String(active)} onChange={k => setTab(Number(k))}
          options={Array.from({ length: visible }, (_, i) => ({ key: String(i), label: `${setLabel(i)}${tag(i)}` }))} />
        {(() => {
          const i = active
          const res = progress.results[i]
          const server = serverOf(setup, i)
          return (
            <div className="board-set" key={i}>
              <div className="board-set-head">
                <span className="board-goal">
                  a
                  <button type="button" className="secondary" aria-label={`Menos tantos en ${setLabel(i)}`} onClick={() => changeGoal(i, -1)}>−</button>
                  <b aria-live="polite">{setup.goals[i]}</b>
                  <button type="button" className="secondary" aria-label={`Más tantos en ${setLabel(i)}`} onClick={() => changeGoal(i, 1)}>+</button>
                  tantos
                </span>
                {res.winner === null
                  ? <button type="button" className="secondary board-finish" onClick={() => void finishSet(i)}>Set terminado</button>
                  : res.byTime && <button type="button" className="secondary board-finish" onClick={() => reopenSet(i)}>Reabrir set</button>}
              </div>
              {i === 2 && (
                <ServeDraw home={home} away={away} homeColor={setup.homeColor} value={setup.serveTie} label="Lanzar moneda del desempate"
                  onResult={side => updateSetup({ ...setup, serveTie: side })} />
              )}
              <div className="board-sides">
                {(['h', 'a'] as const).map((side: Side) => {
                  const col = colorOf(setup, side)
                  return (
                    <div className={`board-side ${col ?? ''}${res.winner === side ? ' won' : ''}`} key={side}>
                      {res.winner === side && <span className="board-win">{res.byTime ? 'Gana por tiempo' : 'Gana el set'}</span>}
                      {sideTitle(nameOf(side), server === side)}
                      <button type="button" className="secondary board-arrow" disabled={res.winner !== null}
                        aria-label={`Subir un punto a ${nameOf(side)}, ${setLabel(i)}`} onClick={() => change(i, side, 1)}>▲</button>
                      <strong className="board-pts" aria-live="polite">{board[i][side]}</strong>
                      <button type="button" className="secondary board-arrow" aria-label={`Bajar un punto a ${nameOf(side)}, ${setLabel(i)}`}
                        onClick={() => change(i, side, -1)}>▼</button>
                    </div>
                  )
                })}
              </div>
            </div>
          )
        })()}
        {error && <p className="error">{error}</p>}
        <div className="row center">
          <button className="secondary" disabled={busy || (board.every(p => p.h === 0 && p.a === 0) && !setup.finished.some(Boolean))} onClick={() => void zero()}>Limpiar</button>
        </div>
        <p className="muted center">El marcador se guarda en este dispositivo mientras juegas.</p>
      </FoldSection>

      {progress.over && (
        <FoldSection title="Resultado" open={open.result} onToggle={() => toggle('result')}
          summary={<span className="fold-text">Ganan {winnerName} ({progress.wins.h}–{progress.wins.a})</span>}>
          <ul className="result-sets">
            {Array.from({ length: visible }, (_, i) => {
              const r = progress.results[i]
              return (
                <li key={i}>
                  <span>{setLabel(i)}</span>
                  <strong>{board[i].h}-{board[i].a}</strong>
                  <span className="muted">{r.winner ? `gana ${nameOf(r.winner)}${r.byTime ? ' (por tiempo)' : ''}` : ''}</span>
                </li>
              )
            })}
          </ul>
          <p className="center"><strong>Ganan {winnerName}</strong> ({home} {progress.wins.h} – {progress.wins.a} {away})</p>
          <BallPicker value={ball} onChange={setBallChoice} />
          {error && <p className="error">{error}</p>}
          <div className="row center">
            <button disabled={busy} onClick={() => void send()}>Enviar resultado</button>
          </div>
          <p className="muted center">El administrador debe aprobar el resultado.</p>
        </FoldSection>
      )}
    </>
  )
}
