import { useEffect, useRef, useState } from 'react'
import type { Side, SideColor } from '../matchSetup'

interface Props {
  home: string
  away: string
  /** Color de la pareja local (la visitante lleva el contrario); `null` si aún no se han elegido los colores. */
  homeColor: SideColor | null
  /** Quién saca ahora (resultado del último sorteo) o `null` si aún no se ha sorteado. */
  value: Side | null
  onResult: (side: Side) => void
  /** Texto del botón la primera vez. */
  label?: string
}

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
/** El lanzamiento dura 3,5 segundos. */
const TOSS_MS = 3500

/**
 * Sorteo del saque con una moneda roja y azul: se lanza, sube, da vueltas y cae con un pequeño bote; el color que queda a
 * la vista es la pareja que saca. Cada cara lleva el nombre de la pareja de ese color, así que hacen falta los colores
 * elegidos. Sin movimiento (si el móvil pide reducirlo) el resultado sale directo.
 */
export default function ServeDraw({ home, away, homeColor, value, onResult, label = 'Lanzar moneda' }: Props) {
  const [drawing, setDrawing] = useState(false)
  const coin = useRef<HTMLDivElement>(null)
  const shadow = useRef<HTMLDivElement>(null)
  const rotation = useRef(0) // grados que lleva girada la moneda (múltiplo de 180: 0 = cara roja, 180 = cara azul)
  const alive = useRef(true)
  useEffect(() => { alive.current = true; return () => { alive.current = false } }, [])

  const colorOfSide = (s: Side): SideColor | null => (homeColor === null ? null : s === 'h' ? homeColor : (homeColor === 'red' ? 'blue' : 'red'))
  const sideOfColor = (c: SideColor): Side => (colorOfSide('h') === c ? 'h' : 'a')
  const redName = homeColor === 'blue' ? away : home // con colores sin elegir, el orden da igual: la moneda está desactivada
  const blueName = homeColor === 'blue' ? home : away

  // En reposo la moneda enseña la cara de quien saca (o la roja si aún no hay sorteo).
  useEffect(() => {
    if (drawing || !coin.current) return
    const server: SideColor | null = value && homeColor ? (value === 'h' ? homeColor : homeColor === 'red' ? 'blue' : 'red') : null
    const base = Math.round(rotation.current / 360) * 360
    rotation.current = base + (server === 'blue' ? 180 : 0)
    coin.current.style.transform = `rotateY(${rotation.current}deg)`
  }, [value, drawing, homeColor])

  async function toss() {
    if (drawing || homeColor === null || !coin.current) return
    setDrawing(true)
    const redWins = Math.random() < 0.5
    const winnerColor: SideColor = redWins ? 'red' : 'blue'
    const from = rotation.current
    const base = Math.round(from / 360) * 360
    const target = base + (8 + Math.floor(Math.random() * 3)) * 360 + (redWins ? 0 : 180)
    if (!prefersReducedMotion()) {
      const c = coin.current
      const a = c.animate([
        { transform: `translateY(0) rotateY(${from}deg)` },
        { transform: `translateY(-120px) rotateY(${from + (target - from) * 0.5}deg)`, offset: 0.45, easing: 'ease-in' },
        { transform: `translateY(0) rotateY(${target}deg)`, offset: 0.82, easing: 'ease-out' },
        { transform: `translateY(-14px) rotateY(${target}deg)`, offset: 0.91 },
        { transform: `translateY(0) rotateY(${target}deg)` },
      ], { duration: TOSS_MS, easing: 'cubic-bezier(.3,0,.5,1)', fill: 'forwards' })
      shadow.current?.animate([
        { transform: 'scaleX(1)', opacity: 0.18 }, { transform: 'scaleX(.5)', opacity: 0.08, offset: 0.45 },
        { transform: 'scaleX(1)', opacity: 0.18, offset: 0.82 }, { transform: 'scaleX(.9)', offset: 0.91 }, { transform: 'scaleX(1)' },
      ], { duration: TOSS_MS })
      await a.finished.catch(() => undefined)
      if (!alive.current) return
      c.style.transform = `rotateY(${target}deg)`
      a.cancel()
      try { navigator.vibrate?.(40) } catch { /* sin vibración */ }
    }
    rotation.current = target
    setDrawing(false)
    onResult(sideOfColor(winnerColor))
  }

  const nameClass = (s: Side) => `${colorOfSide(s) ?? ''}${value === s && !drawing ? ' win' : ''}`
  const status = homeColor === null ? 'Primero elige o sortea los colores de las parejas.'
    : drawing ? 'Lanzando la moneda…'
    : value ? `Cae en ${colorOfSide(value) === 'red' ? 'rojo' : 'azul'}: saca ${value === 'h' ? home : away}.`
    : 'Aún sin sortear: el color que salga es la pareja que saca.'

  return (
    <div className="serve-draw">
      <div className="coin-stage" role="img" aria-label={value ? `Saca ${value === 'h' ? home : away}` : 'Moneda roja y azul para sortear el saque'}>
        <div className="coin-shadow" ref={shadow} />
        <div className="coin" ref={coin}>
          <div className="coin-face red">{redName}</div>
          <div className="coin-face blue">{blueName}</div>
        </div>
      </div>
      <div className="serve-names">
        <span className={nameClass('h')}>{home}</span>
        <span className={nameClass('a')}>{away}</span>
      </div>
      <p className="muted serve-status" role="status" aria-live="polite">{status}</p>
      <button type="button" disabled={drawing || homeColor === null} onClick={() => void toss()}>{value ? 'Volver a lanzar' : label}</button>
    </div>
  )
}

