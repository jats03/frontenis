import type { ClothingSize } from '../api/types'
import { isIncomplete, sizeCounts } from '../clothing'

/** Resumen de tallas de todos los jugadores: recuento por talla (para el pedido) y lista por jugador. */
export default function ClothingSizesSummary({ rows }: { rows: ClothingSize[] }) {
  const done = rows.filter(r => !isIncomplete(r)).length
  return (
    <>
      <p className="muted">{done} de {rows.length} jugadores con las dos tallas</p>
      <h2 className="section-title">Camisetas</h2>
      <Chips rows={sizeCounts(rows, r => r.topSize)} />
      <h2 className="section-title">Pantalones</h2>
      <Chips rows={sizeCounts(rows, r => r.bottomSize)} />
      <h2 className="section-title">Por jugador</h2>
      <div className="card">
        <table className="size-table">
          <thead><tr><th>Jugador</th><th>Camiseta</th><th>Pantalón</th></tr></thead>
          <tbody>
            {rows.map(r => (
              <tr key={r.playerId}>
                <td>{r.name}</td>
                <td className={r.topSize ? '' : 'size-missing'}>{r.topSize ?? 'Falta'}</td>
                <td className={r.bottomSize ? '' : 'size-missing'}>{r.bottomSize ?? 'Falta'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  )
}

function Chips({ rows }: { rows: { size: string; count: number }[] }) {
  if (rows.length === 0) return <p className="muted">Aún no hay ninguna.</p>
  return <ul className="size-chips">{rows.map(c => <li key={c.size}><strong>{c.size}</strong> × {c.count}</li>)}</ul>
}
