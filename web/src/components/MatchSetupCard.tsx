import { useEffect, useRef, useState } from 'react'
import { colorName, colorOf, type MatchSetup, type SideColor } from '../matchSetup'
import ServeDraw from './ServeDraw'

interface Props {
  home: string
  away: string
  setup: MatchSetup
  onChange: (next: MatchSetup) => void
}

const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false

/**
 * «Colores y saque»: rojo o azul para cada pareja (al elegir uno, la otra se queda con el otro), un sorteo de colores con
 * animación (las filas giran entre rojo y azul cada vez más despacio) y el sorteo del saque inicial.
 */
export default function MatchSetupCard({ home, away, setup, onChange }: Props) {
  const [spinning, setSpinning] = useState<SideColor | null>(null) // color que muestra ahora la pareja local mientras gira el sorteo
  const [pop, setPop] = useState(false)
  const timers = useRef<number[]>([])
  useEffect(() => () => timers.current.forEach(window.clearTimeout), [])

  const homeColor = spinning ?? setup.homeColor
  const shown = { ...setup, homeColor }
  const choose = (c: SideColor) => { setPop(false); onChange({ ...setup, homeColor: c }) }

  function drawColors() {
    if (spinning) return
    const finalColor: SideColor = Math.random() < 0.5 ? 'red' : 'blue'
    setPop(false)
    if (prefersReducedMotion()) { onChange({ ...setup, homeColor: finalColor }); return }
    let cur: SideColor = Math.random() < 0.5 ? 'red' : 'blue'
    let delay = 70, i = 0
    const steps = 11
    const tick = () => {
      cur = cur === 'red' ? 'blue' : 'red'
      setSpinning(cur)
      i++
      if (i < steps) { timers.current.push(window.setTimeout(tick, delay)); delay = Math.round(delay * 1.18) }
      else timers.current.push(window.setTimeout(() => {
        setSpinning(null); setPop(true); onChange({ ...setup, homeColor: finalColor })
        try { navigator.vibrate?.(30) } catch { /* sin vibración */ }
      }, delay))
    }
    tick()
  }

  const row = (side: 'h' | 'a', name: string) => {
    const c = colorOf(shown, side)
    return (
      <div className={`setup-team ${c ?? ''}${spinning ? ' spin' : ''}${pop && !spinning ? ' pop' : ''}`} key={`${side}-${spinning ?? ''}`}>
        <span className="setup-name">{name}</span>
        <span className="setup-colors" role="group" aria-label={`Color de ${name}`}>
          {(['red', 'blue'] as const).map(col => (
            <button key={col} type="button" className={`secondary setup-color ${col}${c === col ? ' on' : ''}`} aria-pressed={c === col}
              disabled={!!spinning} onClick={() => choose(side === 'h' ? col : (col === 'red' ? 'blue' : 'red'))}>
              {col === 'red' ? 'Rojo' : 'Azul'}
            </button>
          ))}
        </span>
      </div>
    )
  }

  return (
    <div className="match-setup">
      {row('h', home)}
      {row('a', away)}
      <div className="row center">
        <button type="button" className="secondary" disabled={!!spinning} onClick={drawColors}>{setup.homeColor ? 'Volver a sortear colores' : 'Sortear colores'}</button>
      </div>
      {setup.homeColor && !spinning && (
        <p className="muted center" role="status">{home}: {colorName(setup.homeColor)} · {away}: {colorName(setup.homeColor === 'red' ? 'blue' : 'red')}</p>
      )}
      <ServeDraw home={home} away={away} homeColor={setup.homeColor} value={setup.serveStart}
        onResult={side => onChange({ ...setup, serveStart: side })} />
    </div>
  )
}
