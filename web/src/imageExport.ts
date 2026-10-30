import type { Group, GroupStandings, MatchDto, Slot } from './api/types'
import { formatDate, formatTime } from './dates'
import { resultText, setsText } from './pdf'

/** Imagen (PNG) de la clasificación o de los partidos, para guardarla en la galería o compartirla. */

interface Style { bg: string; fg: string }
interface Table {
  title?: string
  head: string[]
  rows: string[][]
  /** Ancho relativo de cada columna. */
  widths: number[]
  aligns: ('left' | 'center')[]
  rowStyles?: (Style | null)[]
  /** 1 = sube, -1 = baja (flecha al final de la 2.ª columna). */
  marks?: number[]
}
interface Block { heading?: string; tables: Table[] }

const W = 1000, PAD = 40, ROW = 40, SCALE = 2
const HEAD_BG = '#1e3a8a'
const UP: Style = { bg: '#d1fae5', fg: '#065f46' }
const DOWN: Style = { bg: '#fee2e2', fg: '#991b1b' }
const FONT = '16px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
const BOLD = 'bold 16px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'

async function loadLogo(): Promise<HTMLImageElement | null> {
  return new Promise(resolve => {
    const img = new Image()
    img.onload = () => resolve(img)
    img.onerror = () => resolve(null)
    img.src = '/logo.png'
  })
}

const LINE = 20, MAX_LINES = 2, ARROW_SPACE = 26

/** Parte el texto en líneas que quepan en `max`; si pasa de MAX_LINES, la última se corta con «…». */
function wrap(ctx: CanvasRenderingContext2D, text: string, max: number): string[] {
  const words = text.split(' ')
  const lines: string[] = []
  let cur = ''
  for (const w of words) {
    const next = cur ? `${cur} ${w}` : w
    if (ctx.measureText(next).width <= max || !cur) cur = next
    else { lines.push(cur); cur = w }
  }
  if (cur) lines.push(cur)
  const out = lines.slice(0, MAX_LINES)
  if (lines.length > MAX_LINES) out[MAX_LINES - 1] = `${out[MAX_LINES - 1]} ${lines.slice(MAX_LINES).join(' ')}`
  return out.map(l => {
    if (ctx.measureText(l).width <= max) return l
    // una palabra más larga que la columna, o la última línea que se pasa: se corta con «…»
    let s = l
    while (s.length > 1 && ctx.measureText(`${s}…`).width > max) s = s.slice(0, -1)
    return `${s}…`
  })
}

interface RowLayout { lines: string[][]; height: number }

function layoutTable(ctx: CanvasRenderingContext2D, t: Table, width: number): { xs: number[]; rows: RowLayout[] } {
  const inner = width - PAD * 2
  const total = t.widths.reduce((acc, c) => acc + c, 0)
  const xs: number[] = []
  let acc = PAD
  for (const w of t.widths) { xs.push(acc); acc += (w / total) * inner }
  xs.push(PAD + inner)
  const hasMarks = !!t.marks
  const rows = t.rows.map(cells => {
    ctx.font = FONT
    const lines = cells.map((c, i) => {
      const reserve = hasMarks && i === 1 ? ARROW_SPACE : 0
      return wrap(ctx, c, xs[i + 1] - xs[i] - 20 - reserve)
    })
    return { lines, height: ROW + (Math.max(...lines.map(l => l.length), 1) - 1) * LINE }
  })
  return { xs, rows }
}

async function render(title: string, subtitle: string, blocks: Block[], width = W): Promise<Blob> {
  const logo = await loadLogo()
  const measure = document.createElement('canvas').getContext('2d')!
  const layouts = new Map<Table, ReturnType<typeof layoutTable>>()
  const headerH = 110
  let height = headerH + PAD
  for (const b of blocks) {
    if (b.heading) height += 44
    for (const t of b.tables) {
      const l = layoutTable(measure, t, width)
      layouts.set(t, l)
      height += (t.title ? 34 : 0) + ROW + l.rows.reduce((s, r) => s + r.height, 0) + 24
    }
  }
  const canvas = document.createElement('canvas')
  canvas.width = width * SCALE
  canvas.height = height * SCALE
  const ctx = canvas.getContext('2d')!
  ctx.scale(SCALE, SCALE)
  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, width, height)
  ctx.textBaseline = 'middle'

  if (logo) ctx.drawImage(logo, PAD, 24, 70, 70)
  const tx = logo ? PAD + 90 : PAD
  ctx.fillStyle = '#111827'
  ctx.font = 'bold 30px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
  ctx.fillText(title, tx, 48, width - tx - PAD)
  ctx.fillStyle = '#6b7280'
  ctx.font = '18px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
  ctx.fillText(subtitle, tx, 82)

  let y = headerH + 10
  const inner = width - PAD * 2
  for (const b of blocks) {
    if (b.heading) {
      ctx.fillStyle = '#111827'
      ctx.font = 'bold 24px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
      ctx.fillText(b.heading, PAD, y + 18)
      y += 44
    }
    for (const t of b.tables) {
      if (t.title) {
        ctx.fillStyle = '#111827'
        ctx.font = 'bold 18px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
        ctx.fillText(t.title, PAD, y + 14)
        y += 34
      }
      const { xs, rows } = layouts.get(t)!
      const drawRow = (lines: string[][], top: number, h: number, bg: string | null, fg: string, font: string) => {
        if (bg) { ctx.fillStyle = bg; ctx.fillRect(PAD, top, inner, h) }
        ctx.fillStyle = fg
        ctx.font = font
        lines.forEach((cellLines, i) => {
          const startY = top + h / 2 - ((cellLines.length - 1) * LINE) / 2
          cellLines.forEach((text, k) => {
            if (t.aligns[i] === 'center') { ctx.textAlign = 'center'; ctx.fillText(text, xs[i] + (xs[i + 1] - xs[i]) / 2, startY + k * LINE) }
            else { ctx.textAlign = 'left'; ctx.fillText(text, xs[i] + 10, startY + k * LINE) }
          })
        })
        ctx.textAlign = 'left'
      }
      drawRow(t.head.map(h => [h]), y, ROW, HEAD_BG, '#ffffff', BOLD)
      y += ROW
      rows.forEach((r, i) => {
        const s = t.rowStyles?.[i] ?? null
        drawRow(r.lines, y, r.height, s ? s.bg : i % 2 ? '#f3f4f6' : null, s ? s.fg : '#111827', FONT)
        const mark = t.marks?.[i] ?? 0
        if (mark !== 0) {
          // Flecha siempre visible al final de la celda del nombre.
          const cx = xs[2] - 18, cy = y + r.height / 2, sz = 7
          ctx.fillStyle = mark > 0 ? '#065f46' : '#991b1b'
          ctx.beginPath()
          if (mark > 0) { ctx.moveTo(cx - sz, cy + sz * 0.8); ctx.lineTo(cx + sz, cy + sz * 0.8); ctx.lineTo(cx, cy - sz) }
          else { ctx.moveTo(cx - sz, cy - sz * 0.8); ctx.lineTo(cx + sz, cy - sz * 0.8); ctx.lineTo(cx, cy + sz) }
          ctx.closePath()
          ctx.fill()
        }
        y += r.height
      })
      y += 24
    }
  }
  return new Promise((resolve, reject) => canvas.toBlob(b => (b ? resolve(b) : reject(new Error('No se pudo crear la imagen'))), 'image/png'))
}

/** En el móvil abre el menú de compartir (permite «Guardar imagen» en la galería); en el ordenador descarga el PNG. */
async function deliver(blob: Blob, filename: string) {
  const file = new File([blob], filename, { type: 'image/png' })
  const touch = window.matchMedia?.('(pointer: coarse)').matches
  if (touch && navigator.canShare?.({ files: [file] })) {
    try { await navigator.share({ files: [file] }); return } catch (e) {
      if (e instanceof DOMException && e.name === 'AbortError') return
    }
  }
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 5000)
}

const safe = (name: string, fallback: string) => name.replace(/[^\w\- ]+/g, '').trim().replace(/\s+/g, '_') || fallback

export async function exportStandingsImage(competitionName: string, standings: GroupStandings[], individual = false) {
  const blocks: Block[] = standings.map(g => ({
    tables: [{
      title: g.groupName,
      head: ['#', individual ? 'Jugador' : 'Pareja', 'PJ', 'G', 'P', 'TF', 'TC', 'Dif', 'Pts'],
      rows: g.rows.map(r => [String(r.rank), r.pairName, String(r.played), String(r.won), String(r.lost), String(r.pointsFor),
        String(r.pointsAgainst), r.pointsDiff > 0 ? `+${r.pointsDiff}` : String(r.pointsDiff), String(r.classificationPoints)]),
      widths: [5, 41, 7, 7, 7, 8, 8, 8, 9],
      aligns: ['center', 'left', 'center', 'center', 'center', 'center', 'center', 'center', 'center'],
      rowStyles: g.rows.map(r => (r.movement > 0 ? UP : r.movement < 0 ? DOWN : null)),
      marks: g.rows.map(r => r.movement),
    }],
  }))
  await deliver(await render(competitionName, 'Clasificación', blocks), `clasificacion_${safe(competitionName, 'clasificacion')}.png`)
}

export async function exportMatchesImage(competitionName: string, matches: MatchDto[], groups: Group[], round?: number) {
  const list = matches.filter(m => m.phase === 'Group' && (round === undefined || m.round === round))
  const rounds = [...new Set(list.map(m => m.round))].sort((a, b) => a - b)
  const blocks: Block[] = rounds.map(r => ({
    heading: `Jornada ${r}`,
    tables: groups.flatMap(g => {
      const ms = list.filter(m => m.round === r && m.groupId === g.id).sort((a, b) => a.order - b.order)
      if (ms.length === 0) return []
      return [{
        title: groups.length > 1 ? g.name : undefined,
        head: ['Pareja A', 'Resultado', 'Pareja B', 'Sets'],
        rows: ms.map(m => [m.homeName ?? '', resultText(m), m.awayName ?? '', setsText(m)]),
        widths: [32, 12, 32, 24],
        aligns: ['left', 'center', 'left', 'left'] as ('left' | 'center')[],
      }]
    }),
  }))
  const sub = round === undefined ? 'Partidos' : `Partidos · Jornada ${round}`
  await deliver(await render(competitionName, sub, blocks),
    `partidos_${safe(competitionName, 'competicion')}${round === undefined ? '' : `_jornada_${round}`}.png`)
}

/**
 * Imagen de los apuntados a cada entreno, tal como se ven en la app, para mandarla por WhatsApp en vez de la
 * plantilla de texto (`reminders.ts`, que se conserva). Las franjas se agrupan por día.
 */
export async function exportAttendanceImage(title: string, subtitle: string, slots: Slot[], filename: string) {
  const days = new Map<string, Slot[]>()
  for (const s of [...slots].sort((a, b) => a.date.localeCompare(b.date) || a.start.localeCompare(b.start))) {
    days.set(s.date, [...(days.get(s.date) ?? []), s])
  }
  const blocks: Block[] = [...days.entries()].map(([date, list]) => ({
    heading: formatDate(date),
    tables: list.map(s => ({
      title: `${formatTime(s.start)} a ${formatTime(s.end)} · ${s.attendees.length === 1 ? '1 apuntado' : `${s.attendees.length} apuntados`}`,
      head: ['#', 'Jugador'],
      rows: s.attendees.length > 0 ? s.attendees.map((a, i) => [String(i + 1), a.name]) : [['', 'Nadie apuntado todavía']],
      widths: [8, 92],
      aligns: ['center', 'left'] as ('left' | 'center')[],
    })),
  }))
  await deliver(await render(title, subtitle, blocks, 560), filename)
}
