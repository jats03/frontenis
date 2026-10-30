import { api } from '../api/client'
import type { DeviceMe } from '../api/types'
import { useAsync } from '../hooks'
import ClothingSizesPanel from './ClothingSizesPanel'
import LoadingBlock from './LoadingBlock'
import PageHeader from './PageHeader'

/** Tallas de ropa de quien tiene identificado su dispositivo y de sus apadrinados. */
export default function MyClothingSizes({ me }: { me: DeviceMe }) {
  const sizes = useAsync(() => api.device.sizes(), [me.owner.id, me.delegates.length])
  return (
    <>
      <PageHeader title="Tallas de ropa" subtitle="Camiseta y pantalón, tuyos y de tus apadrinados" />
      {sizes.error && <p className="error">{sizes.error}</p>}
      {!sizes.data && !sizes.error && <LoadingBlock />}
      {sizes.data && <ClothingSizesPanel rows={sizes.data} onChange={r => sizes.setData(() => r)} />}
    </>
  )
}
