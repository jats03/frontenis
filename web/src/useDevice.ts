import { useCallback, useEffect, useState } from 'react'
import { api } from './api/client'
import type { DeviceMe } from './api/types'

/**
 * Estado del vínculo de este dispositivo con su jugador.
 * `me`: `undefined` mientras carga, `null` si no está vinculado.
 */
export function useDevice(enabled = true) {
  const [me, setMe] = useState<DeviceMe | null | undefined>(undefined)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    if (!enabled) { setMe(null); return }
    try { setMe((await api.device.me()) ?? null); setError(null) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error'); setMe(null) }
  }, [enabled])

  useEffect(() => { void reload() }, [reload])

  const link = useCallback(async (playerId: number) => {
    setMe(await api.device.link(playerId))
  }, [])

  const unlink = useCallback(async () => {
    await api.device.unlink()
    setMe(null)
  }, [])

  return { me, error, reload, link, unlink }
}
