import type { SlotStatus, WeekDay } from './api/types'

/** Días de la semana en el orden del club (lunes primero). */
export const WEEK_DAYS: { value: WeekDay; label: string }[] = [
  { value: 'Monday', label: 'Lunes' },
  { value: 'Tuesday', label: 'Martes' },
  { value: 'Wednesday', label: 'Miércoles' },
  { value: 'Thursday', label: 'Jueves' },
  { value: 'Friday', label: 'Viernes' },
  { value: 'Saturday', label: 'Sábado' },
  { value: 'Sunday', label: 'Domingo' },
]

export const weekDayLabel = (day: WeekDay) => WEEK_DAYS.find(d => d.value === day)?.label ?? day

export const slotStatusLabel: Record<SlotStatus, string> = {
  Pending: 'Aún sin abrir',
  Open: 'Abierta',
  Closed: 'Ya empezó',
  Cancelled: 'Cancelada',
}
