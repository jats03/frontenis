import { useEffect, useMemo, useRef, useState } from 'react'
import type { PointerEvent } from 'react'
import { api } from '../api/client'
import type { HistoryFilter } from '../api/types'
import { cardFromHistory, positionCode } from '../cardStats'
import type { CardTier } from '../cardStats'
import { useAsync } from '../hooks'
import LoadingBlock from './LoadingBlock'
import PlayerAvatar from './PlayerAvatar'
import { DEFAULT_BALL } from '../balls'

const ALL: HistoryFilter = { competitionIds: [], trainingIds: [], ball: DEFAULT_BALL }

const TIER_RANK: Record<CardTier, number> = { bronze: 0, silver: 1, gold: 2 }
const TIER_NAME: Record<CardTier, string> = { bronze: 'bronce', silver: 'plata', gold: 'oro' }
const CONFETTI_COLORS = ['#f6e27a', '#ffffff', '#9be3d9', '#f28c6b', '#c79a2b']
/** En iOS el giroscopio pide permiso, y solo se puede pedir desde un toque del usuario (p. ej. al abrir la carta). */
export function askMotionPermission() {
  try {
    const ctor = (typeof DeviceOrientationEvent !== 'undefined' ? DeviceOrientationEvent : undefined) as
      (typeof DeviceOrientationEvent & { requestPermission?: () => Promise<string> }) | undefined
    void ctor?.requestPermission?.().catch(() => undefined)
  } catch { /* sin giroscopio */ }
}
const clamp = (n: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, n))
const calm = () => typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches

/** Compara con el último nivel que este dispositivo vio de ese jugador; devuelve el nivel nuevo solo si ha subido. */
function tierRise(playerId: number, tier: CardTier): CardTier | null {
  const key = `frontenis.cardTier.${playerId}`
  try {
    const before = localStorage.getItem(key) as CardTier | null
    localStorage.setItem(key, tier)
    return before && before in TIER_RANK && TIER_RANK[tier] > TIER_RANK[before] ? tier : null
  } catch { return null }
}

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
  const tiltRef = useRef<HTMLDivElement>(null)
  const [rise, setRise] = useState<CardTier | null>(null)
  const confetti = useMemo(() => Array.from({ length: 36 }, (_, i) => ({
    left: Math.random() * 100, delay: Math.random() * .6, dur: 1.8 + Math.random() * 1.4,
    drift: Math.round(Math.random() * 120 - 60), spin: Math.round(Math.random() * 720 - 360),
    color: CONFETTI_COLORS[i % CONFETTI_COLORS.length],
  })), [])
  const tier = card?.played ? card.tier : null
  useEffect(() => { if (tier) setRise(tierRise(playerId, tier)) }, [playerId, tier])

  // En el móvil la carta también sigue al giro del teléfono (la posición inicial es la «plana»).
  const hasCard = card !== null
  useEffect(() => {
    if (!hasCard || calm()) return
    let base: { b: number; g: number } | null = null
    const onTurn = (e: DeviceOrientationEvent) => {
      const el = tiltRef.current
      if (!el || el.classList.contains('tilting') || e.beta === null || e.gamma === null) return
      base ??= { b: e.beta, g: e.gamma }
      const ry = clamp(e.gamma - base.g, -15, 15)
      const rx = clamp(base.b - e.beta, -15, 15)
      el.style.setProperty('--ry', `${ry.toFixed(1)}deg`)
      el.style.setProperty('--rx', `${rx.toFixed(1)}deg`)
      el.style.setProperty('--mx', `${(50 + ry * 3).toFixed(0)}%`)
      el.style.setProperty('--my', `${(40 - rx * 3).toFixed(0)}%`)
    }
    window.addEventListener('deviceorientation', onTurn)
    return () => window.removeEventListener('deviceorientation', onTurn)
  }, [hasCard])

  /** Inclina la carta hacia el puntero y mueve el reflejo; se desactiva con «reducir movimiento». */
  function tilt(e: PointerEvent<HTMLDivElement>) {
    if (calm()) return
    const el = e.currentTarget
    const r = el.getBoundingClientRect()
    const x = (e.clientX - r.left) / r.width
    const y = (e.clientY - r.top) / r.height
    el.style.setProperty('--ry', `${((x - .5) * 24).toFixed(1)}deg`)
    el.style.setProperty('--rx', `${((.5 - y) * 24).toFixed(1)}deg`)
    el.style.setProperty('--mx', `${(x * 100).toFixed(0)}%`)
    el.style.setProperty('--my', `${(y * 100).toFixed(0)}%`)
    el.classList.add('tilting')
  }
  function untilt(e: PointerEvent<HTMLDivElement>) {
    const el = e.currentTarget
    el.classList.remove('tilting')
    for (const v of ['--rx', '--ry', '--mx', '--my']) el.style.removeProperty(v)
  }
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
        <div className="fut-tilt" ref={tiltRef} onPointerDown={tilt} onPointerMove={tilt} onPointerLeave={untilt} onPointerCancel={untilt}>
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
        </div>
      )}
      {rise && (
        <>
          <p className="fut-levelup" role="status">¡Sube a {TIER_NAME[rise]}!</p>
          <div className="fut-confetti" aria-hidden="true">
            {confetti.map((c, i) => (
              <i key={i} style={{ left: `${c.left}%`, background: c.color, animationDelay: `${c.delay}s`, animationDuration: `${c.dur}s`,
                '--drift': `${c.drift}px`, '--spin': `${c.spin}deg` } as React.CSSProperties} />
            ))}
          </div>
        </>
      )}
      {summary && <button type="button" className="secondary fut-close" onClick={() => ref.current?.close()}>Cerrar</button>}
    </dialog>
  )
}
