import type { Group, GroupStandings, MatchDto } from './api/types'
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

function fit(ctx: CanvasRenderingContext2D, text: string, max: number): string {
  if (ctx.measureText(text).width <= max) return text
  let t = text
  while (t.length > 1 && ctx.measureText(`${t}…`).width > max) t = t.slice(0, -1)
  return `${t}…`
}

async function render(title: string, subtitle: string, blocks: Block[]): Promise<Blob> {
  const logo = await loadLogo()
  const headerH = 110
  let height = headerH + PAD
  for (const b of blocks) {
    if (b.heading) height += 44
    for (const t of b.tables) height += (t.title ? 34 : 0) + ROW * (t.rows.length + 1) + 24
  }
  const canvas = document.createElement('canvas')
  canvas.width = W * SCALE
  canvas.height = height * SCALE
  const ctx = canvas.getContext('2d')!
  ctx.scale(SCALE, SCALE)
  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, W, height)
  ctx.textBaseline = 'middle'

  if (logo) ctx.drawImage(logo, PAD, 24, 70, 70)
  const tx = logo ? PAD + 90 : PAD
  ctx.fillStyle = '#111827'
  ctx.font = 'bold 30px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
  ctx.fillText(fit(ctx, title, W - tx - PAD), tx, 48)
  ctx.fillStyle = '#6b7280'
  ctx.font = '18px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif'
  ctx.fillText(subtitle, tx, 82)

  let y = headerH + 10
  const inner = W - PAD * 2
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
      const total = t.widths.reduce((a, c) => a + c, 0)
      const xs: number[] = []
      let acc = PAD
      for (const w of t.widths) { xs.push(acc); acc += (w / total) * inner }
      xs.push(PAD + inner)
      const drawRow = (cells: string[], top: number, bg: string | null, fg: string, font: string) => {
        if (bg) { ctx.fillStyle = bg; ctx.fillRect(PAD, top, inner, ROW) }
        ctx.fillStyle = fg
        ctx.font = font
        cells.forEach((c, i) => {
          const colW = xs[i + 1] - xs[i] - 20
          const text = fit(ctx, c, colW)
          if (t.aligns[i] === 'center') { ctx.textAlign = 'center'; ctx.fillText(text, xs[i] + (xs[i + 1] - xs[i]) / 2, top + ROW / 2) }
          else { ctx.textAlign = 'left'; ctx.fillText(text, xs[i] + 10, top + ROW / 2) }
        })
        ctx.textAlign = 'left'
      }
      drawRow(t.head, y, HEAD_BG, '#ffffff', BOLD)
      y += ROW
      t.rows.forEach((r, i) => {
        const s = t.rowStyles?.[i] ?? null
        drawRow(r, y, s ? s.bg : i % 2 ? '#f3f4f6' : null, s ? s.fg : '#111827', FONT)
        y += ROW
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
