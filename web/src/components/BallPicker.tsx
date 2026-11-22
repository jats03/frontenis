import { BALLS, type BallType } from '../balls'

interface Props {
  value: BallType
  onChange: (ball: BallType) => void
  /** Añade la opción «Todas» (suma de todas las bolas). Con `selected`, ninguna bola concreta aparece marcada. */
  withAll?: { selected: boolean; onSelect: () => void }
}

/** Elección de la bola del partido: tres botones pequeños (una sola opción marcada). */
export default function BallPicker({ value, onChange, withAll }: Props) {
  return (
    <div className="ball-picker" role="radiogroup" aria-label="Bola del partido">
      <span className="ball-picker-label" aria-hidden="true">Bola</span>
      {withAll && (
        <button type="button" role="radio" aria-checked={withAll.selected}
          className={`ball-tag ball-All${withAll.selected ? ' on' : ''}`} onClick={withAll.onSelect}>
          Todas
          {withAll.selected && <small className="ball-sel" aria-hidden="true">(seleccionada)</small>}
        </button>
      )}
      {BALLS.map(b => (
        <button key={b.key} type="button" role="radio" aria-checked={!withAll?.selected && value === b.key}
          className={`ball-tag ball-${b.key}${!withAll?.selected && value === b.key ? ' on' : ''}`} onClick={() => onChange(b.key)}>
          {b.label}
          {!withAll?.selected && value === b.key && <small className="ball-sel" aria-hidden="true">(seleccionada)</small>}
        </button>
      ))}
    </div>
  )
}
