import type { Slot } from './api/types'
import { formatTime, mondayOfIso, parseDate, todayIso } from './dates'

/** Solo se puede pedir que se apunten a franjas con las inscripciones abiertas. */
export const isRemindable = (slot: Slot) => slot.status === 'Open'

const capitalize = (t: string) => t.charAt(0).toUpperCase() + t.slice(1)
const dayLabel = (iso: string) => capitalize(parseDate(iso).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' }))
const dayShort = (iso: string) => capitalize(parseDate(iso).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric' }))
const range = (s: Slot) => `${formatTime(s.start)}–${formatTime(s.end)}`
const people = (n: number) => (n === 0 ? 'todavía nadie apuntado' : n === 1 ? '1 apuntado' : `${n} apuntados`)

/** Mensaje para recordar al grupo que se apunten a un entreno concreto. */
export function slotReminder(slot: Slot, link: string): string {
  const need = slot.fewPlayers ? ' Faltan jugadores.' : ''
  return `Entreno del ${dayLabel(slot.date).toLowerCase()}, ${range(slot)}: ${people(slot.attendeeCount)}.${need}\n` +
    `¡Apúntate! ${link}`
}

/** Mensaje para recordar al grupo que se apunten a los entrenos abiertos de una semana (lunes a domingo). */
export function weekReminder(slots: Slot[], mondayIso: string, link: string): string {
  const open = slots.filter(isRemindable).sort((a, b) => a.date.localeCompare(b.date) || a.start.localeCompare(b.start))
  const current = mondayOfIso(todayIso()) === mondayIso
  const title = current ? 'Recordatorio: apúntate a los entrenos de esta semana'
    : `Recordatorio: apúntate a los entrenos de la semana del ${parseDate(mondayIso).toLocaleDateString('es-ES', { day: 'numeric', month: 'long' })}`
  const lines = open.map(s => `• ${dayShort(s.date)}, ${range(s)} (${people(s.attendeeCount)})`)
  return [title, '', ...lines, '', `Apúntate aquí: ${link}`].join('\n')
}

/** Enlace que abre WhatsApp con el texto ya escrito; la persona elige el grupo y pulsa enviar. */
export const whatsappShareUrl = (text: string) => `https://wa.me/?text=${encodeURIComponent(text)}`

/** Dirección de la página de reservas, a la que lleva el recordatorio. */
export const reservasLink = () => `${window.location.origin}/reservas`
