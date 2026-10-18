import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from './api/client'
import type { DeviceMe } from './api/types'
import { useAuth } from './auth/AuthContext'
import { hasDeviceToken } from './device'
import { DeviceContext } from './useDevice'

/**
 * Mantiene en un solo sitio quién es la persona de este dispositivo, para que la cabecera y las páginas
 * muestren lo mismo. El administrador no se vincula. Si este dispositivo nunca se ha identificado
 * (no tiene identificador guardado) no se llama a la API.
 */
export function DeviceProvider({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  const [me, setMe] = useState<DeviceMe | null | undefined>(undefined)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    if (isAdmin || !hasDeviceToken()) { setMe(null); return }
    try { setMe((await api.device.me()) ?? null); setError(null) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error'); setMe(null) }
  }, [isAdmin])

  useEffect(() => { void reload() }, [reload])

  const link = useCallback(async (playerId: number) => { setMe(await api.device.link(playerId)) }, [])
  const unlink = useCallback(async () => { await api.device.unlink(); setMe(null) }, [])

  const value = useMemo(() => ({ me, error, reload, link, unlink }), [me, error, reload, link, unlink])
  return <DeviceContext.Provider value={value}>{children}</DeviceContext.Provider>
}
