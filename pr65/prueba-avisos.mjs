import { signUpWeeks } from './signUpNotices.ts'
const me = { id: 1, name: 'Ana' }, luis = { id: 2, name: 'Luis' }
// Hoy miércoles 2026-11-11 a las 19:30. Semana en curso: lun 9 – dom 15. Próxima: lun 16 – dom 22.
const S = (id, date, start, status, ids = []) => ({ id, date, start, end: '20:00:00', opensAt: '', status, attendees: ids.map(i => ({ playerId: i, name: String(i) })) })
const hoy = '2026-11-11', ahora = '19:30:00'
const casos = [
  ['1. Esta semana: lunes ya terminado (Ana apuntada) y jueves abierto sin Ana', [S(1,'2026-11-09','18:00:00','Closed',[1]), S(2,'2026-11-12','18:00:00','Open',[])], [me], ['current:Ana']],
  ['2. Esta semana: Ana apuntada al jueves (abierto)', [S(1,'2026-11-09','18:00:00','Closed',[]), S(2,'2026-11-12','18:00:00','Open',[1])], [me], []],
  ['3. Esta semana: solo quedan entrenos ya empezados (hoy 18:00 sigue Open en la API pero ya empezó) -> sin aviso', [S(1,'2026-11-11','18:00:00','Open',[]), S(2,'2026-11-09','18:00:00','Closed',[])], [me], []],
  ['4. Esta semana: hoy 21:00 aún no ha empezado -> avisa', [S(1,'2026-11-11','21:00:00','Open',[])], [me], ['current:Ana']],
  ['5. Próxima semana abierta y Ana sin apuntar (esta semana ya apuntada)', [S(1,'2026-11-12','18:00:00','Open',[1]), S(2,'2026-11-16','18:00:00','Open',[])], [me], ['next:Ana']],
  ['6. Próxima semana todavía sin abrir (Pending) -> sin aviso', [S(1,'2026-11-12','18:00:00','Open',[1]), S(2,'2026-11-16','18:00:00','Pending',[])], [me], []],
  ['7. Las dos semanas sin apuntar: dos avisos', [S(1,'2026-11-12','18:00:00','Open',[]), S(2,'2026-11-16','18:00:00','Open',[])], [me], ['current:Ana','next:Ana']],
  ['8. Con apadrinado: Luis sin apuntar esta semana', [S(1,'2026-11-12','18:00:00','Open',[1])], [me, luis], ['current:Luis']],
  ['9. Entreno cancelado de la próxima semana no cuenta', [S(1,'2026-11-12','18:00:00','Open',[1]), S(2,'2026-11-16','18:00:00','Cancelled',[])], [me], []],
  ['10. Domingo 15 y lunes siguiente: la semana cambia correctamente', [S(1,'2026-11-15','18:00:00','Open',[]), S(2,'2026-11-16','18:00:00','Open',[1])], [me], ['current:Ana']],
]
let fallos = 0
for (const [n, slots, people, esperado] of casos) {
  const got = signUpWeeks(slots, people, hoy, ahora).map(w => `${w.key}:${w.missing.map(p => p.name).join('+')}`)
  const ok = JSON.stringify(got) === JSON.stringify(esperado)
  if (!ok) fallos++
  console.log(`${ok ? 'OK   ' : 'FALLA'} ${n}\n      esperado: [${esperado}] | obtenido: [${got}]`)
}
console.log(`\n${casos.length - fallos} de ${casos.length} casos correctos`); process.exit(fallos ? 1 : 0)
