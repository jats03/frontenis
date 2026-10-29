import { BALLS, type BallType } from '../balls'

interface Props { value: BallType[]; onChange: (balls: BallType[]) => void }

/**
 * Filtro por bola: se pueden marcar una o varias, pero siempre queda al menos una marcada
 * (pulsar la última que queda no hace nada).
 */
export default function BallFilter({ value, onChange }: Props) {
  function toggle(ball: BallType) {
    if (value.includes(ball)) {
      if (value.length === 1) return
      onChange(value.filter(b => b !== ball))
    } else {
      onChange(BALLS.map(b => b.key).filter(k => k === ball || value.includes(k))) // siempre en el mismo orden
    }
  }
  return (
    <div className="ball-picker" role="group" aria-label="Bolas que cuentan">
      <span className="ball-picker-label" aria-hidden="true">Bola</span>
      {BALLS.map(b => (
        <button key={b.key} type="button" aria-pressed={value.includes(b.key)}
          className={`ball-tag ball-${b.key}${value.includes(b.key) ? ' on' : ''}`} onClick={() => toggle(b.key)}>{b.label}</button>
      ))}
    </div>
  )
}
