import { useMemo, useState } from 'react'
import { notify } from '../alerts'
import { useDocumentTitle } from '../hooks'
import {
  buildTemplate, MONTHS, STORAGE_KEY, weekOfMonth, weeksInMonth, WEEKDAYS, mondayOfWeek,
  type DayPlan, type Slot,
} from '../whatsapp'

const defaultDays = (): DayPlan[] => {
  const one = (): Slot[] => [{ label: '', spots: 6 }]
  return WEEKDAYS.map((name, weekday) => {
    if (weekday === 1) return { name, weekday, enabled: true, slots: [{ label: '17H a 18h', spots: 6 }, { label: '18h a 19h', spots: 6 }, { label: '20h a 22h', spots: 6 }] }
    if (weekday === 3) return { name, weekday, enabled: true, slots: [{ label: '17H a 18h', spots: 6 }, { label: '18h a 19h', spots: 7 }, { label: '20h a 22h', spots: 6 }] }
    if (weekday === 4) return { name, weekday, enabled: true, slots: one() }
    return { name, weekday, enabled: false, slots: one() }
  })
}

interface Saved { title: string; footer: string; bold: boolean; days: DayPlan[] }
function loadSaved(): Saved | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    const parsed = raw ? (JSON.parse(raw) as Saved) : null
    return parsed && Array.isArray(parsed.days) && parsed.days.length === 7 ? parsed : null
  } catch { return null }
}

export default function WhatsappPage() {
  useDocumentTitle('Plantillas de WhatsApp')
  const today = new Date()
  const saved = useMemo(loadSaved, [])
  const [title, setTitle] = useState(saved?.title ?? 'ENTRENAMIENTOS')
  const [footer, setFooter] = useState(saved?.footer ?? '#SOMOSFR🟡NTENIS')
  const [bold, setBold] = useState(saved?.bold ?? false)
  const [days, setDays] = useState<DayPlan[]>(saved?.days ?? defaultDays())
  const [month, setMonth] = useState(today.getMonth())
  const [year, setYear] = useState(today.getFullYear())
  const [week, setWeek] = useState(weekOfMonth(today))
  const [edited, setEdited] = useState<string | null>(null)

  const weeks = weeksInMonth(year, month)
  const safeWeek = Math.min(week, weeks)
  const generated = useMemo(
    () => buildTemplate({ title, month, year, week: safeWeek, days, footer, bold }),
    [title, month, year, safeWeek, days, footer, bold])
  const text = edited ?? generated
  const monday = mondayOfWeek(year, month, safeWeek)

  function persist(next: Partial<Saved>) {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify({ title, footer, bold, days, ...next })) } catch { /* sin almacenamiento */ }
  }
  const change = <T,>(set: (v: T) => void, key: keyof Saved) => (v: T) => { set(v); setEdited(null); persist({ [key]: v } as Partial<Saved>) }
  const setDaysP = change(setDays, 'days')

  const updateDay = (i: number, patch: Partial<DayPlan>) => setDaysP(days.map((d, k) => (k === i ? { ...d, ...patch } : d)))
  const updateSlot = (i: number, j: number, patch: Partial<Slot>) =>
    updateDay(i, { slots: days[i].slots.map((s, k) => (k === j ? { ...s, ...patch } : s)) })

  async function copy() {
    try { await navigator.clipboard.writeText(text); notify('Plantilla copiada: ya puedes pegarla en WhatsApp') }
    catch {
      const el = document.getElementById('wa-text') as HTMLTextAreaElement | null
      el?.select(); document.execCommand('copy'); notify('Plantilla copiada')
    }
  }

  return (
    <>
      <h1>Plantillas de WhatsApp</h1>
      <p className="muted">Elige el mes y la semana, ajusta días y horarios, y copia el texto para pegarlo en WhatsApp.</p>

      <section className="card form">
        <div className="cols">
          <label>Título<input value={title} onChange={e => change(setTitle, 'title')(e.target.value)} /></label>
          <label>Mes
            <select value={month} onChange={e => { setMonth(Number(e.target.value)); setEdited(null) }}>
              {MONTHS.map((m, i) => <option key={m} value={i}>{m.charAt(0) + m.slice(1).toLowerCase()}</option>)}
            </select>
          </label>
          <label>Año
            <input type="number" min={2020} max={2100} value={year}
              onChange={e => { setYear(e.target.valueAsNumber || today.getFullYear()); setEdited(null) }} />
          </label>
          <label>Semana
            <select value={safeWeek} onChange={e => { setWeek(Number(e.target.value)); setEdited(null) }}>
              {Array.from({ length: weeks }, (_, i) => i + 1).map(w => <option key={w} value={w}>{w}.ª semana</option>)}
            </select>
          </label>
        </div>
        <p className="muted">
          Semana del {monday.toLocaleDateString('es-ES', { day: 'numeric', month: 'long' })} al{' '}
          {new Date(monday.getFullYear(), monday.getMonth(), monday.getDate() + 6).toLocaleDateString('es-ES', { day: 'numeric', month: 'long' })}.
        </p>

        <fieldset className="wa-days">
          <legend>Días y horarios</legend>
          {days.map((d, i) => (
            <div key={d.weekday} className="wa-day">
              <label className="check">
                <input type="checkbox" checked={d.enabled} onChange={e => updateDay(i, { enabled: e.target.checked })} />
                <strong>{d.name.charAt(0) + d.name.slice(1).toLowerCase()}</strong>
              </label>
              {d.enabled && (
                <div className="wa-slots">
                  {d.slots.map((s, j) => (
                    <div key={j} className="wa-slot">
                      <input aria-label={`Horario ${j + 1} del ${d.name.toLowerCase()} (vacío = sin horario)`} placeholder="Horario (opcional)"
                        value={s.label} onChange={e => updateSlot(i, j, { label: e.target.value })} />
                      <input type="number" min={0} max={50} aria-label={`Plazas del horario ${j + 1} del ${d.name.toLowerCase()}`}
                        value={s.spots} onChange={e => updateSlot(i, j, { spots: e.target.valueAsNumber || 0 })} />
                      <span className="muted">plazas</span>
                      <button type="button" className="secondary" aria-label={`Quitar horario ${j + 1} del ${d.name.toLowerCase()}`}
                        disabled={d.slots.length === 1}
                        onClick={() => updateDay(i, { slots: d.slots.filter((_, k) => k !== j) })}>✕</button>
                    </div>
                  ))}
                  <button type="button" className="secondary"
                    onClick={() => updateDay(i, { slots: [...d.slots, { label: '', spots: 6 }] })}>+ Añadir horario</button>
                </div>
              )}
            </div>
          ))}
        </fieldset>

        <label>Texto final<input value={footer} onChange={e => change(setFooter, 'footer')(e.target.value)} /></label>
        <label className="check">
          <input type="checkbox" checked={bold} onChange={e => change(setBold, 'bold')(e.target.checked)} />
          Títulos en negrita de WhatsApp (*texto*)
        </label>
      </section>

      <section className="card">
        <h2>Resultado</h2>
        <label>Texto de la plantilla (puedes editarlo antes de copiar)
          <textarea id="wa-text" className="wa-text" rows={22} value={text} onChange={e => setEdited(e.target.value)} />
        </label>
        <div className="row">
          <button onClick={() => void copy()}>Copiar para WhatsApp</button>
          <a className="button" target="_blank" rel="noopener noreferrer" href={`https://wa.me/?text=${encodeURIComponent(text)}`}>Abrir en WhatsApp</a>
          {edited !== null && <button className="secondary" onClick={() => setEdited(null)}>Restaurar texto generado</button>}
        </div>
      </section>
    </>
  )
}
