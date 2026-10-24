import type { Group, GroupStandings, MatchDto } from './api/types'

async function loadLogo(): Promise<string | null> {
  try {
    const blob = await (await fetch('/logo.png')).blob()
    return await new Promise<string>((resolve, reject) => {
      const r = new FileReader()
      r.onload = () => resolve(r.result as string)
      r.onerror = reject
      r.readAsDataURL(blob)
    })
  } catch { return null }
}

const safeName = (name: string, fallback: string) => name.replace(/[^\w\- ]+/g, '').trim().replace(/\s+/g, '_') || fallback

/** Resultado en sets ganados («2-1»). */
export function resultText(m: MatchDto): string {
  if (!m.isPlayed) return '—'
  return `${m.homeSets ?? 1}-${m.awaySets ?? 0}`
}

/** Tantos de cada set («15-5 / 14-15 / 5-10»). */
export function setsText(m: MatchDto): string {
  if (!m.isPlayed) return ''
  if (m.sets && m.sets.length > 0) return m.sets.map(s => `${s.home}-${s.away}`).join(' / ')
  return `${m.homeScore}-${m.awayScore}`
}

/** PDF con los partidos de la fase de grupos, una sección por jornada (todas, o solo `round`) y una tabla por grupo. */
export async function exportMatchesPdf(competitionName: string, matches: MatchDto[], groups: Group[], round?: number) {
  const [{ jsPDF }, { default: autoTable }, logo] = await Promise.all([
    import('jspdf'), import('jspdf-autotable'), loadLogo(),
  ])
  const doc = new jsPDF({ unit: 'mm', format: 'a4' })
  const group = matches.filter(m => m.phase === 'Group' && (round === undefined || m.round === round))
  const rounds = [...new Set(group.map(m => m.round))].sort((a, b) => a - b)

  if (logo) doc.addImage(logo, 'PNG', 14, 10, 22, 22)
  doc.setFontSize(18)
  doc.text(competitionName, logo ? 40 : 14, 19)
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text(round === undefined ? 'Partidos' : `Partidos · Jornada ${round}`, logo ? 40 : 14, 26)
  doc.setTextColor(0)

  let y = 38
  const lastY = () => (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY
  for (const r of rounds) {
    if (y > 250) { doc.addPage(); y = 20 }
    doc.setFontSize(14)
    doc.text(`Jornada ${r}`, 14, y)
    y += 5
    for (const g of groups) {
      const list = group.filter(m => m.round === r && m.groupId === g.id).sort((a, b) => a.order - b.order)
      if (list.length === 0) continue
      if (y > 255) { doc.addPage(); y = 20 }
      if (groups.length > 1) { doc.setFontSize(11); doc.text(g.name, 14, y + 2); y += 4 }
      autoTable(doc, {
        startY: y,
        head: [['Pareja A', 'Resultado', 'Pareja B', 'Sets']],
        body: list.map(m => [m.homeName ?? '', resultText(m), m.awayName ?? '', setsText(m)]),
        headStyles: { fillColor: [30, 58, 138] },
        columnStyles: { 1: { halign: 'center' } },
        // Todo alineado a la izquierda salvo el resultado, que va centrado (también su cabecera).
        styles: { halign: 'left', fontSize: 10 },
        margin: { left: 14, right: 14 },
      } as Parameters<typeof autoTable>[1])
      y = lastY() + 6
    }
    y += 4
  }

  doc.save(`partidos_${safeName(competitionName, 'competicion')}${round === undefined ? '' : `_jornada_${round}`}.pdf`)
}

export async function exportStandingsPdf(competitionName: string, standings: GroupStandings[], individual = false) {
  const [{ jsPDF }, { default: autoTable }, logo] = await Promise.all([
    import('jspdf'), import('jspdf-autotable'), loadLogo(),
  ])
  const doc = new jsPDF({ unit: 'mm', format: 'a4' })
  const pageWidth = doc.internal.pageSize.getWidth()

  if (logo) doc.addImage(logo, 'PNG', 14, 10, 22, 22)
  doc.setFontSize(18)
  doc.text(competitionName, logo ? 40 : 14, 19)
  doc.setFontSize(10)
  doc.setTextColor(100)
  doc.text('Clasificación', logo ? 40 : 14, 26)
  doc.setTextColor(0)

  let y = 38
  for (const g of standings) {
    doc.setFontSize(13)
    doc.text(g.groupName, 14, y)
    autoTable(doc, {
      startY: y + 3,
      head: [['#', individual ? 'Jugador' : 'Pareja', 'PJ', 'G', 'P', 'TF', 'TC', 'Dif', 'Pts']],
      body: g.rows.map(r => [
        r.rank, r.pairName, r.played, r.won, r.lost, r.pointsFor, r.pointsAgainst,
        r.pointsDiff > 0 ? `+${r.pointsDiff}` : r.pointsDiff, r.classificationPoints,
      ]),
      headStyles: { fillColor: [30, 58, 138] },
      columnStyles: { 1: { halign: 'left' } },
      // Liga rotatoria terminada: en verde quien sube y en rojo quien baja.
      didParseCell: data => {
        if (data.section !== 'body') return
        const movement = g.rows[data.row.index]?.movement ?? 0
        if (movement > 0) { data.cell.styles.fillColor = [209, 250, 229]; data.cell.styles.textColor = [6, 95, 70] }
        else if (movement < 0) { data.cell.styles.fillColor = [254, 226, 226]; data.cell.styles.textColor = [153, 27, 27] }
      },
      styles: { halign: 'center', fontSize: 10 },
      margin: { left: 14, right: 14 },
      width: pageWidth,
    } as Parameters<typeof autoTable>[1])
    y = (doc as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 10
    if (y > 260) { doc.addPage(); y = 20 }
  }

  const safe = competitionName.replace(/[^\w\- ]+/g, '').trim().replace(/\s+/g, '_') || 'clasificacion'
  doc.save(`clasificacion_${safe}.pdf`)
}
