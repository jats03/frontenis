import type { Slot } from './api/types'
import { formatTime, mondayOfIso, parseDate } from './dates'
import { buildTemplate, loadTemplateStyle, weekOfMonth, WEEKDAYS, type DayPlan } from './whatsapp'

/** Solo se puede pedir que se apunten a franjas con las inscripciones abiertas. */
export const isRemindable = (slot: Slot) => slot.status === 'Open'

/** Líneas numeradas mínimas por franja, como en la plantilla de WhatsApp. */
const MIN_SPOTS = 6

/** «18:00:00» → «18h»; «18:30:00» → «18:30h». */
const hour = (time: string) => {
  const t = formatTime(time)
  return t.endsWith(':00') ? `${Number(t.slice(0, 2))}h` : `${t}h`
}
const label = (s: Slot) => `${hour(s.start)} a ${hour(s.end)}`
const byTime = (a: Slot, b: Slot) => a.date.localeCompare(b.date) || a.start.localeCompare(b.start)

/** Día de la semana de una fecha: 0 = lunes … 6 = domingo. */
const weekdayOf = (iso: string) => (parseDate(iso).getDay() + 6) % 7

/** Agrupa las franjas por día con el formato de la plantilla de WhatsApp (apuntados en las líneas numeradas). */
function toDays(slots: Slot[]): DayPlan[] {
  const days = new Map<number, DayPlan>()
  for (const s of [...slots].sort(byTime)) {
    const weekday = weekdayOf(s.date)
    const day = days.get(weekday) ?? { name: WEEKDAYS[weekday], weekday, enabled: true, slots: [] }
    day.slots.push({ label: label(s), spots: Math.max(MIN_SPOTS, s.attendeeCount), names: s.attendees.map(a => a.name) })
    days.set(weekday, day)
  }
  return [...days.values()]
}

/** Año, mes y semana del mes (la 1.ª es la que contiene el día 1) de la semana que empieza el lunes dado. */
function weekPosition(mondayIso: string) {
  const sunday = parseDate(mondayIso)
  sunday.setDate(sunday.getDate() + 6)
  return { year: sunday.getFullYear(), month: sunday.getMonth(), week: weekOfMonth(sunday) }
}

function build(slots: Slot[], mondayIso: string, link: string, showWeek: boolean): string {
  const { year, month, week } = weekPosition(mondayIso)
  const style = loadTemplateStyle()
  const footer = style.footer.trim() ? `*${style.footer.trim()}*` : ''
  return buildTemplate({
    ...style, footer, bold: true, year, month, week, days: toDays(slots), showWeek,
    closing: ['¡Apuntaros!', link],
  })
}

/** Mensaje con el mismo formato que la plantilla de WhatsApp para recordar que se apunten a un entreno concreto. */
export const slotReminder = (slot: Slot, link: string): string => build([slot], mondayOfIso(slot.date), link, false)

/** Igual que la plantilla semanal de WhatsApp, con los entrenos abiertos de esa semana (lunes a domingo). */
export function weekReminder(slots: Slot[], mondayIso: string, link: string): string {
  return build(slots.filter(isRemindable), mondayIso, link, true)
}

/** Enlace que abre WhatsApp con el texto ya escrito; la persona elige el grupo y pulsa enviar. */
export const whatsappShareUrl = (text: string) => `https://wa.me/?text=${encodeURIComponent(text)}`

/**
 * Dirección de la página de reservas, a la que lleva el recordatorio. WhatsApp solo convierte en enlace
 * direcciones públicas (con dominio), así que en pruebas locales se puede fijar con `VITE_PUBLIC_URL`
 * (por ejemplo, la web ya publicada); sin ella se usa la dirección desde la que se abre la app.
 */
export const reservasLink = () => {
  const configured = (import.meta.env.VITE_PUBLIC_URL as string | undefined)?.trim().replace(/\/+$/, '')
  return `${configured || window.location.origin}/reservas`
}
