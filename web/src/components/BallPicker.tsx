import { BALLS, type TrainingBall } from '../balls'

interface Props { value: TrainingBall; onChange: (ball: TrainingBall) => void }

/** Elección de la bola del partido: tres botones pequeños (una sola opción marcada). */
export default function BallPicker({ value, onChange }: Props) {
  return (
    <div className="ball-picker" role="radiogroup" aria-label="Bola del partido">
      <span className="ball-picker-label" aria-hidden="true">Bola</span>
      {BALLS.map(b => (
        <button key={b.key} type="button" role="radio" aria-checked={value === b.key}
          className={`ball-tag ball-${b.key}${value === b.key ? ' on' : ''}`} onClick={() => onChange(b.key)}>{b.label}</button>
      ))}
    </div>
  )
}
