import type { GroupStandings } from './api/types'

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
        r.rank, r.pairName + (r.movement > 0 ? '  (sube)' : r.movement < 0 ? '  (baja)' : ''), r.played, r.won, r.lost, r.pointsFor, r.pointsAgainst,
        r.pointsDiff > 0 ? `+${r.pointsDiff}` : r.pointsDiff, r.classificationPoints,
      ]),
      headStyles: { fillColor: [15, 118, 110] },
      columnStyles: { 1: { halign: 'left' } },
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
