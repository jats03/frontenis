import { useEffect, useRef, useState } from 'react'
import { otherSide, type Side, type SideColor } from '../matchSetup'
import { Racket } from './RacketLoader'

const LEFT_X = 47, RIGHT_X = 113, BALL_Y = 52
/** Dónde acaba la pelota cuando una pareja no llega a devolverla: pasa de largo por detrás de su raqueta. */
const OUT_LEFT = 6, OUT_RIGHT = 154
const RACKET_REST = 'rotate(14deg)'

interface Props {
  home: string
  away: string
  homeColor: SideColor | null
  /** Quién saca ahora (resultado del último sorteo) o `null` si aún no se ha sorteado. */
  value: Side | null
  onResult: (side: Side) => void
  /** Texto del botón la primera vez. */
  label?: string
}

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
const sideX = (s: Side) => (s === 'h' ? LEFT_X : RIGHT_X)
const placeAt = (x: number) => `translate(${x}px, ${BALL_Y}px)`
const place = (s: Side) => placeAt(sideX(s))

/**
 * Sorteo del saque con la misma animación que el aviso de carga: las dos parejas pelotean (las raquetas se pasan la pelota,
 * cada vez más despacio) y al final una gana el punto: la otra llega tarde y la pelota pasa de largo. La que gana es la que
 * saca. Sin movimiento (si el móvil pide reducirlo) el resultado sale directo.
 */
export default function ServeDraw({ home, away, homeColor, value, onResult, label = 'Sortear saque' }: Props) {
  const [drawing, setDrawing] = useState(false)
  const ballX = useRef<SVGGElement>(null)
  const ballY = useRef<SVGGElement>(null)
  const leftR = useRef<SVGGElement>(null)
  const rightR = useRef<SVGGElement>(null)
  const alive = useRef(true)
  useEffect(() => { alive.current = true; return () => { alive.current = false } }, [])

  // En reposo la pelota está en la raqueta de quien saca (o en la izquierda si aún no hay sorteo).
  useEffect(() => {
    if (drawing || !ballX.current) return
    ballX.current.style.transform = place(value ?? 'h')
  }, [value, drawing])

  const swing = (s: Side, ms: number) => {
    const el = s === 'h' ? leftR.current : rightR.current
    el?.animate([
      { transform: RACKET_REST, easing: 'ease-out' }, { transform: 'rotate(-4deg)', offset: 0.22 },
      { transform: 'rotate(-34deg)', offset: 0.8, easing: 'ease-in' }, { transform: RACKET_REST },
    ], { duration: ms })
  }

  /** Un vuelo de la pelota entre dos puntos, en arco (sube y baja como en la carga). */
  async function fly(fromX: number, toX: number, ms: number) {
    const x = ballX.current, y = ballY.current
    if (!x || !y) return
    const a = x.animate([{ transform: placeAt(fromX) }, { transform: placeAt(toX) }], { duration: ms, easing: 'ease-in-out', fill: 'forwards' })
    const b = y.animate([
      { transform: 'translateY(0)', easing: 'cubic-bezier(.2,.7,.4,1)' },
      { transform: 'translateY(-26px)', offset: 0.5, easing: 'cubic-bezier(.6,0,.8,.3)' },
      { transform: 'translateY(0)' },
    ], { duration: ms })
    await Promise.all([a.finished, b.finished]).catch(() => undefined)
    if (alive.current) x.style.transform = placeAt(toX)
    a.cancel()
  }
  const wait = (ms: number) => new Promise<void>(resolve => window.setTimeout(resolve, ms))

  async function draw() {
    if (drawing) return
    setDrawing(true)
    const winner: Side = Math.random() < 0.5 ? 'h' : 'a'
    if (!prefersReducedMotion()) {
      // La pelota sale de la izquierda y los pases alternan. El último lo da la pareja que gana, y la otra no llega:
      // con un número impar de vuelos gana la izquierda y con uno par, la derecha.
      const flights = (winner === 'h' ? [7, 9] : [8, 10])[Math.random() < 0.5 ? 0 : 1]
      const loser = otherSide(winner)
      let at: Side = 'h'
      if (ballX.current) ballX.current.style.transform = place('h')
      for (let i = 0; i < flights - 1 && alive.current; i++) {
        const to = otherSide(at)
        await fly(sideX(at), sideX(to), Math.round(340 * Math.pow(1.15, i))) // de 0,34 s (como la carga) a ~1 s
        if (!alive.current) return
        swing(to, 360) // devuelve la pelota
        at = to
      }
      if (!alive.current) return
      // Último vuelo: la que gana la manda a la otra, que llega tarde (su raqueta golpea el aire) y la pelota pasa de largo.
      const late = window.setTimeout(() => swing(loser, 360), 520)
      await fly(sideX(winner), loser === 'a' ? OUT_RIGHT : OUT_LEFT, 780)
      window.clearTimeout(late)
      if (!alive.current) return
      swing(winner, 420) // celebra el punto
      try { navigator.vibrate?.(40) } catch { /* sin vibración */ }
      await wait(650)
      if (!alive.current) return
    }
    setDrawing(false)
    onResult(winner)
  }

  const homeCls = homeColor ? (homeColor === 'red' ? 'red' : 'blue') : ''
  const awayCls = homeColor ? (homeColor === 'red' ? 'blue' : 'red') : ''
  return (
    <div className="serve-draw">
      <svg className="rally serve-rally" viewBox="0 0 160 84" role="img" focusable="false"
        aria-label={value ? `Saca ${value === 'h' ? home : away}` : 'Raquetas y pelota para sortear el saque'}>
        <ellipse className="rally-shadow serve-shadow" cx="80" cy="78" rx="6" ry="1.6" fill="#000" />
        <g className={`serve-racket ${homeCls}${value === 'h' && !drawing ? ' win' : ''}`} ref={leftR} style={{ transform: RACKET_REST }}><Racket className="racket-static" /></g>
        <g transform="translate(160 0) scale(-1 1)">
          <g className={`serve-racket ${awayCls}${value === 'a' && !drawing ? ' win' : ''}`} ref={rightR} style={{ transform: RACKET_REST }}><Racket className="racket-static" /></g>
        </g>
        <g ref={ballX} style={{ transform: place(value ?? 'h') }}>
          <g ref={ballY}>
            <circle cx="0" cy="0" r="5" fill="#f2a900" stroke="#b97c00" strokeWidth="1" />
            <path d="M-3 -2.2c1.6 1 4 1 5.8-.2" stroke="#fff" strokeWidth=".9" fill="none" opacity=".75" strokeLinecap="round" />
          </g>
        </g>
      </svg>
      <div className="serve-names">
        <span className={`${homeCls}${value === 'h' && !drawing ? ' win' : ''}`}>{home}</span>
        <span className={`${awayCls}${value === 'a' && !drawing ? ' win' : ''}`}>{away}</span>
      </div>
      <p className="muted serve-status" role="status" aria-live="polite">
        {drawing ? 'Sorteando el saque…' : value ? `Saca ${value === 'h' ? home : away}.` : 'Aún sin sortear.'}
      </p>
      <button type="button" disabled={drawing} onClick={() => void draw()}>{value ? 'Volver a sortear' : label}</button>
    </div>
  )
}
