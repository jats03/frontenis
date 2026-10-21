import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { confirmAction, notify } from '../alerts'
import { api } from '../api/client'
import type { CompetitionDetail, MatchDto, PendingResult, SetScore } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import LoadingBlock from '../components/LoadingBlock'
import StopwatchPanel from '../components/StopwatchPanel'
import { useDocumentTitle } from '../hooks'
import { TIMER_KEY } from '../timerStore'
import { useDevice } from '../useDevice'

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

/** Cuántos sets se muestran: con 2 sets por partido, el 3.º (desempate) solo si cada pareja ganó uno. */
function visibleSets(pts: Pts[], setsPerMatch: number): number {
  if (setsPerMatch !== 2) return 1
  const [s1, s2] = pts
  const split = s1.h !== s1.a && s2.h !== s2.a && (s1.h > s1.a) !== (s2.h > s2.a)
  return split ? 3 : 2
}

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
    if (draft) { setPts(draft); return }
    const base = ZERO()
    if (mine && mine.playerId === playerId) mine.sets.forEach((s, i) => { if (i < 3) base[i] = { h: s.home, a: s.away } })
    setPts(base)
  }, [detail, pts, mine, matchId, playerId])

  function change(i: number, side: 'h' | 'a', delta: number) {
    setPts(prev => {
      if (!prev) return prev
      const next = prev.map((p, k) => (k === i ? { ...p, [side]: Math.min(MAX_POINTS, Math.max(0, p[side] + delta)) } : p))
      saveDraft(matchId, next)
      return next
    })
  }
  function zero() { clearDraft(matchId); setPts(ZERO()); setError(null) }

  const match: MatchDto | undefined = detail?.matches.find(m => m.id === matchId)
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
      <section className="card"><p><strong>Resultado: </strong>{match.sets?.map(s => `${s.home}-${s.away}`).join(', ') ?? `${match.homeScore}-${match.awayScore}`}</p></section></>
  }
  if (match.homeEntryId === null || match.awayEntryId === null) {
    return <>{back}<h1>{home} – {away}</h1><p className="muted">Este partido todavía no tiene las dos parejas definidas.</p></>
  }
  if (mine && mine.playerId !== playerId) {
    return <>{back}<h1>{home} – {away}</h1>
      <section className="card">
        <p><strong>Resultado pendiente de aprobación</strong></p>
        <p>{mine.sets.map(s => `${s.home}-${s.away}`).join(', ')} · enviado por {mine.playerName}</p>
        <p className="muted">Solo quien lo envió puede modificarlo, hasta que el administrador lo apruebe o lo rechace.</p>
      </section></>
  }

  const board = pts ?? ZERO()
  const visible = visibleSets(board, setsPerMatch)

  async function send() {
    const sets: SetScore[] = []
    for (let i = 0; i < visible; i++) {
      const p = board[i]
      if (p.h === p.a) { setError(`El set ${i + 1} no puede acabar en empate.`); return }
      sets.push({ home: p.h, away: p.a })
    }
    setError(null)
    const resumen = sets.map((s, i) => `${visible > 1 ? (i === 2 ? 'Desempate' : `Set ${i + 1}`) + ': ' : ''}${s.home}-${s.away}`).join(' · ')
    const ok = await confirmAction('¿Enviar el resultado?',
      `${home} – ${away}\n${resumen}\nQuedará pendiente de aprobación del administrador.`, 'Sí, enviar', 'Seguir jugando')
    if (!ok) return
    setBusy(true)
    try {
      await api.matches.submitResult(matchId, sets)
      clearDraft(matchId)
      notify('Resultado enviado, pendiente de aprobación')
      await load()
    } catch (e) { setError(e instanceof Error ? e.message : 'Error') }
    finally { setBusy(false) }
  }

  const sideTitle = (name: string) => <span className="board-team">{name}</span>

  return (
    <>
      {back}
      <h1>{home} – {away}</h1>
      {mine && (
        <p className="notice">Resultado enviado ({mine.sets.map(s => `${s.home}-${s.away}`).join(', ')}): pendiente de aprobación del
          administrador. Puedes corregirlo y volver a enviarlo.</p>
      )}

      <StopwatchPanel storageKey={`${TIMER_KEY}.match.${matchId}`} compact />

      <section className="card board">
        <h2>Marcador</h2>
        {Array.from({ length: visible }, (_, i) => (
          <div className="board-set" key={i}>
            {visible > 1 && <div className="set-label">{i === 2 ? 'Desempate' : `Set ${i + 1}`}</div>}
            <div className="board-sides">
              {(['h', 'a'] as const).map(side => (
                <div className="board-side" key={side}>
                  {sideTitle(side === 'h' ? home : away)}
                  <button type="button" className="secondary board-arrow" aria-label={`Subir un punto a ${side === 'h' ? home : away}, set ${i + 1}`}
                    onClick={() => change(i, side, 1)}>▲</button>
                  <strong className="board-pts" aria-live="polite">{board[i][side]}</strong>
                  <button type="button" className="secondary board-arrow" aria-label={`Bajar un punto a ${side === 'h' ? home : away}, set ${i + 1}`}
                    onClick={() => change(i, side, -1)}>▼</button>
                </div>
              ))}
            </div>
          </div>
        ))}
        {error && <p className="error">{error}</p>}
        <div className="row center">
          <button disabled={busy} onClick={() => void send()}>Enviar resultado</button>
          <button className="secondary" disabled={busy || board.every(p => p.h === 0 && p.a === 0)} onClick={zero}>Poner a cero</button>
        </div>
        <p className="muted center">El marcador se guarda en este dispositivo mientras juegas. El administrador debe aprobar el resultado.</p>
      </section>
    </>
  )
}
