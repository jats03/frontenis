/** `names`: jugadores ya apuntados, que se escriben en las primeras líneas numeradas. */
export interface Slot { label: string; spots: number; names?: string[] }
export interface DayPlan { name: string; weekday: number /* 0 = lunes … 6 = domingo */; enabled: boolean; slots: Slot[] }
export interface TemplateOptions {
  title: string
  month: number // 0-11
  year: number
  week: number  // 1..6, semana del mes (lunes a domingo); la 1.ª es la que contiene el día 1
  days: DayPlan[]
  footer: string
  bold: boolean
  /** Por defecto se muestra «N° SEMANA»; los avisos de un solo entreno lo ocultan. */
  showWeek?: boolean
  /** Líneas que se añaden justo antes del pie (por ejemplo, la llamada a apuntarse y el enlace). */
  closing?: string[]
}

export const STORAGE_KEY = 'frontenis.whatsapp'

/** Título, pie y negrita de la plantilla guardada en este navegador (o los valores por defecto). */
export function loadTemplateStyle(): Pick<TemplateOptions, 'title' | 'footer' | 'bold'> {
  const fallback = { title: 'ENTRENAMIENTOS', footer: '#SOMOSFR🟡NTENIS', bold: false }
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    const saved = raw ? (JSON.parse(raw) as Partial<typeof fallback>) : null
    return { title: saved?.title ?? fallback.title, footer: saved?.footer ?? fallback.footer, bold: saved?.bold ?? fallback.bold }
  } catch { return fallback }
}

export const MONTHS = ['ENERO', 'FEBRERO', 'MARZO', 'ABRIL', 'MAYO', 'JUNIO', 'JULIO', 'AGOSTO', 'SEPTIEMBRE', 'OCTUBRE', 'NOVIEMBRE', 'DICIEMBRE']
export const WEEKDAYS = ['LUNES', 'MARTES', 'MIÉRCOLES', 'JUEVES', 'VIERNES', 'SÁBADO', 'DOMINGO']

/** Lunes de la semana n-ésima del mes (la 1.ª semana es la que contiene el día 1). */
export function mondayOfWeek(year: number, month: number, week: number): Date {
  const first = new Date(year, month, 1)
  const offset = (first.getDay() + 6) % 7 // lunes = 0
  return new Date(year, month, 1 - offset + 7 * (week - 1))
}

/** Semana del mes (1..6) a la que pertenece una fecha. */
export function weekOfMonth(d: Date): number {
  const offset = (new Date(d.getFullYear(), d.getMonth(), 1).getDay() + 6) % 7
  return Math.floor((d.getDate() - 1 + offset) / 7) + 1
}

/** Cuántas semanas (lunes a domingo) toca el mes. */
export function weeksInMonth(year: number, month: number): number {
  const offset = (new Date(year, month, 1).getDay() + 6) % 7
  return Math.ceil((new Date(year, month + 1, 0).getDate() + offset) / 7)
}

export function buildTemplate(o: TemplateOptions): string {
  const b = (t: string) => (o.bold ? `*${t.trim()}*` : t)
  const monday = mondayOfWeek(o.year, o.month, o.week)
  const out: string[] = []
  out.push(b(`${o.title} ${MONTHS[o.month]} ${o.year}`.trim()), '')
  if (o.showWeek !== false) out.push(b(` ${o.week}° SEMANA `), '')

  for (const day of o.days.filter(d => d.enabled).sort((a, b2) => a.weekday - b2.weekday)) {
    const date = new Date(monday); date.setDate(monday.getDate() + day.weekday)
    out.push(b(` ${day.name} DÍA ${date.getDate()}`), '')
    day.slots.forEach((slot, i) => {
      if (slot.label.trim()) out.push(b(` ${slot.label.trim()} `))
      for (let n = 1; n <= Math.max(0, slot.spots); n++) out.push(`${n}-${slot.names?.[n - 1] ? ` ${slot.names[n - 1]}` : ''}`)
      out.push('')
      if (i < day.slots.length - 1) out.push('')
    })
    out.push('')
  }
  if (o.closing?.length) out.push(...o.closing, '')
  if (o.footer.trim()) out.push(o.footer.trim())
  return out.join('\n').replace(/\n{4,}/g, '\n\n\n').trimEnd() + '\n'
}
