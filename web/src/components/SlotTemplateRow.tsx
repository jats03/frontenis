import { useState } from 'react'
import type { SaveSlotTemplate, SlotTemplate, WeekDay } from '../api/types'
import { formatTime, toApiTime } from '../dates'
import { WEEK_DAYS, weekDayLabel } from '../slots'

interface Props {
  template: SlotTemplate
  onSave: (changes: SaveSlotTemplate) => void
  onDelete: () => void
}

/** Una fila de la plantilla semanal: se ve como texto y, al editar, como formulario. */
export default function SlotTemplateRow({ template, onSave, onDelete }: Props) {
  const [editing, setEditing] = useState(false)
  const [day, setDay] = useState<WeekDay>(template.day)
  const [start, setStart] = useState(formatTime(template.start))
  const [end, setEnd] = useState(formatTime(template.end))

  if (editing) {
    return (
      <li className="card">
        <div className="cols">
          <label>Día
            <select value={day} onChange={e => setDay(e.target.value as WeekDay)}>
              {WEEK_DAYS.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
            </select>
          </label>
          <label>Desde<input type="time" value={start} onChange={e => setStart(e.target.value)} required /></label>
          <label>Hasta<input type="time" value={end} onChange={e => setEnd(e.target.value)} required /></label>
        </div>
        <div className="row">
          <button type="button"
            onClick={() => { onSave({ day, start: toApiTime(start), end: toApiTime(end), active: template.active }); setEditing(false) }}>
            Guardar
          </button>
          <button type="button" className="secondary" onClick={() => setEditing(false)}>Cancelar</button>
        </div>
      </li>
    )
  }

  return (
    <li className={`card${template.active ? '' : ' inactive'}`}>
      <div className="row between">
        <strong>{weekDayLabel(template.day)} · {formatTime(template.start)}–{formatTime(template.end)}</strong>
        {!template.active && <span className="badge">Desactivada</span>}
      </div>
      <div className="row">
        <button type="button" className="secondary" onClick={() => setEditing(true)}>Editar</button>
        <button type="button" className="secondary"
          onClick={() => onSave({ day: template.day, start: template.start, end: template.end, active: !template.active })}>
          {template.active ? 'Desactivar' : 'Activar'}
        </button>
        <button type="button" className="secondary danger" onClick={onDelete}>Eliminar</button>
      </div>
    </li>
  )
}
