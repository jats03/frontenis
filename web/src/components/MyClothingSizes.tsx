import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { api } from '../api/client'
import type { DeviceMe } from '../api/types'
import { useAsync } from '../hooks'
import ClothingSizesPanel from './ClothingSizesPanel'

/** Tallas de ropa de quien tiene identificado su dispositivo y de sus apadrinados (sección de «Más»). */
export default function MyClothingSizes({ me }: { me: DeviceMe }) {
  const sizes = useAsync(() => api.device.sizes(), [me.owner.id, me.delegates.length])
  const { hash } = useLocation()
  const loaded = sizes.data !== null

  // Al llegar desde el aviso de Inicio, se baja hasta esta sección.
  useEffect(() => {
    if (loaded && hash === '#tallas') document.getElementById('tallas')?.scrollIntoView({ block: 'start' })
  }, [loaded, hash])

  if (sizes.error) return <p className="error">{sizes.error}</p>
  if (!sizes.data) return null
  return (
    <>
      <h2 className="section-title" id="tallas">Tallas de ropa</h2>
      <ClothingSizesPanel rows={sizes.data} onChange={r => sizes.setData(() => r)} />
    </>
  )
}
