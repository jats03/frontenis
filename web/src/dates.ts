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
