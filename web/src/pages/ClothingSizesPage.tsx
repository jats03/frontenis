import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import ClothingSizesSummary from '../components/ClothingSizesSummary'
import LoadingBlock from '../components/LoadingBlock'
import MyClothingSizes from '../components/MyClothingSizes'
import PageHeader from '../components/PageHeader'
import SizeViewersPanel from '../components/SizeViewersPanel'
import { useAsync, useDocumentTitle } from '../hooks'
import { useDevice } from '../useDevice'

/**
 * Tallas de ropa. Quien tiene su dispositivo identificado indica las suyas y las de sus apadrinados; el administrador,
 * y los jugadores a los que da acceso, ven además el resumen de todos (el recuento por talla sirve para el pedido).
 */
export default function ClothingSizesPage() {
  useDocumentTitle('Tallas de ropa')
  const { isAdmin } = useAuth()
  const device = useDevice()

  if (isAdmin) return <AdminSizes />
  if (device.me === undefined) return <LoadingBlock />
  if (!device.me) return <><h1>Tallas de ropa</h1><p className="error">Identifícate en Inicio para indicar tus tallas.</p></>
  return (
    <>
      <MyClothingSizes me={device.me} />
      {device.me.canViewSizes && <Everybody />}
    </>
  )
}

/** Resumen de todos los jugadores para quien el administrador ha autorizado. */
function Everybody() {
  const all = useAsync(() => api.device.allSizes(), [])
  return (
    <>
      <h2 className="section-title">Todos los jugadores</h2>
      {all.error && <p className="error">{all.error}</p>}
      {!all.data && !all.error && <LoadingBlock />}
      {all.data && <ClothingSizesSummary rows={all.data} />}
    </>
  )
}

function AdminSizes() {
  const sizes = useAsync(() => api.clothingSizes.all(), [])
  return (
    <>
      <PageHeader title="Tallas de ropa" subtitle="Camiseta y pantalón de cada jugador" />
      {sizes.error && <p className="error">{sizes.error}</p>}
      {sizes.loading && !sizes.data && <LoadingBlock />}
      {sizes.data && <ClothingSizesSummary rows={sizes.data} />}
      <SizeViewersPanel />
    </>
  )
}
