/** Fechas de la app: siempre en hora local y en español. */
const pad = (n: number) => String(n).padStart(2, '0')

export const todayIso = () => {
  const d = new Date()
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** Admite «2026-10-13» o «2026-10-13T00:00:00» (sin zona) y los interpreta como fecha local. */
export const parseDate = (iso: string) => new Date(iso.length === 10 ? `${iso}T00:00:00` : iso)

export const formatDate = (iso: string) =>
  parseDate(iso).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })

export const formatShortDate = (iso: string) =>
  parseDate(iso).toLocaleDateString('es-ES', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' })

/** «18:00:00» → «18:00». */
export const formatTime = (time: string) => time.slice(0, 5)

/** Los inputs <input type="time"> dan «18:00»; la API espera «18:00:00». */
export const toApiTime = (time: string) => (time.length === 5 ? `${time}:00` : time)

export const addDaysIso = (iso: string, days: number) => {
  const d = parseDate(iso)
  d.setDate(d.getDate() + days)
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

/** Lunes de la semana de la fecha dada (la semana del club va de lunes a domingo). */
export const mondayOfIso = (iso: string) => addDaysIso(iso, -((parseDate(iso).getDay() + 6) % 7))

/** «viernes, 16 de octubre, 19:00» a partir de una fecha-hora local sin zona. */
export const formatOpening = (iso: string) =>
  `${parseDate(iso).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' })}, ${iso.slice(11, 16)}`
