/** Temporadas (septiembre-agosto) y meses para dividir la lista de entrenamientos. */

/** Año en que empieza la temporada de una fecha «AAAA-MM-DD»: de septiembre a agosto (2026-09 a 2027-08 = 2026/2027). */
export const seasonStart = (iso: string) => {
  const year = Number(iso.slice(0, 4)), month = Number(iso.slice(5, 7))
  return month >= 9 ? year : year - 1
}

export const seasonLabel = (start: number) => `${start}/${start + 1}`

/** «2026-10» de una fecha. */
export const monthKey = (iso: string) => iso.slice(0, 7)

/** «Octubre» a partir de «2026-10». */
export const monthName = (key: string) => {
  const name = new Date(`${key}-01T00:00:00`).toLocaleDateString('es-ES', { month: 'long' })
  return name.charAt(0).toUpperCase() + name.slice(1)
}

/** Temporadas con entrenamientos más la actual, de la más reciente a la más antigua. */
export function seasonsOf(dates: string[], today: string): number[] {
  return [...new Set([...dates.map(seasonStart), seasonStart(today)])].sort((a, b) => b - a)
}

/** Meses con entrenamientos de una temporada, en orden cronológico. */
export function monthsOf(dates: string[], season: number): string[] {
  return [...new Set(dates.filter(d => seasonStart(d) === season).map(monthKey))].sort()
}

/** Mes que se muestra por defecto: el actual; si no tiene entrenamientos, el último anterior y, si no, el más reciente. */
export function defaultMonth(months: string[], today: string): string | undefined {
  const now = monthKey(today)
  if (months.includes(now)) return now
  const before = months.filter(m => m < now)
  return before.length ? before[before.length - 1] : months[months.length - 1]
}
