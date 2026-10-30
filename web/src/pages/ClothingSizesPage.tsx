import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import LoadingBlock from '../components/LoadingBlock'
import MyClothingSizes from '../components/MyClothingSizes'
import PageHeader from '../components/PageHeader'
import { isIncomplete, sizeCounts } from '../clothing'
import { useAsync, useDocumentTitle } from '../hooks'
import { useDevice } from '../useDevice'

/**
 * Tallas de ropa. El administrador ve las de todos los jugadores con el recuento por talla (para el pedido);
 * quien tiene su dispositivo identificado indica las suyas y las de sus apadrinados.
 */
export default function ClothingSizesPage() {
  useDocumentTitle('Tallas de ropa')
  const { isAdmin } = useAuth()
  const device = useDevice()
  const sizes = useAsync(() => (isAdmin ? api.clothingSizes.all() : Promise.resolve([])), [isAdmin])

  if (!isAdmin) {
    if (device.me === undefined) return <LoadingBlock />
    if (device.me) return <MyClothingSizes me={device.me} />
    return <><h1>Tallas de ropa</h1><p className="error">Identifícate en Inicio para indicar tus tallas.</p></>
  }
  const rows = sizes.data ?? []
  const missing = rows.filter(isIncomplete)

  return (
    <>
      <PageHeader title="Tallas de ropa" subtitle={`${rows.length - missing.length} de ${rows.length} jugadores con las dos tallas`} />
      {sizes.error && <p className="error">{sizes.error}</p>}
      {sizes.loading && !sizes.data && <LoadingBlock />}
      {sizes.data && (
        <>
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
      )}
    </>
  )
}

function Chips({ rows }: { rows: { size: string; count: number }[] }) {
  if (rows.length === 0) return <p className="muted">Aún no hay ninguna.</p>
  return <ul className="size-chips">{rows.map(c => <li key={c.size}><strong>{c.size}</strong> × {c.count}</li>)}</ul>
}
