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
const ME_KEY = 'frontenis.me'

/** Última respuesta conocida de «¿quién soy?» en este navegador (para decidir al instante, sin esperar a la API). */
function readCachedMe(): DeviceMe | null | undefined {
  try {
    const raw = localStorage.getItem(ME_KEY)
    return raw === null ? undefined : (JSON.parse(raw) as DeviceMe | null)
  } catch { return undefined }
}
function cacheMe(me: DeviceMe | null) {
  try { localStorage.setItem(ME_KEY, JSON.stringify(me)) } catch { /* sin almacenamiento */ }
}

export function DeviceProvider({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  // Sin identificador guardado no puede estar vinculado: no se espera a la API. Con identificador, se parte de lo último
  // conocido (si lo hay) y se confirma por detrás; solo se espera a la API si no hay nada guardado.
  const [me, setMe] = useState<DeviceMe | null | undefined>(() => (isAdmin || !hasDeviceToken() ? null : readCachedMe()))
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    if (isAdmin || !hasDeviceToken()) { setMe(null); return }
    try { const m = (await api.device.me()) ?? null; cacheMe(m); setMe(m); setError(null) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error'); setMe(null) }
  }, [isAdmin])

  useEffect(() => { void reload() }, [reload])

  const link = useCallback(async (playerId: number, replace = false) => { const m = await api.device.link(playerId, replace); cacheMe(m); setMe(m) }, [])
  const unlink = useCallback(async () => { await api.device.unlink(); cacheMe(null); setMe(null) }, [])

  const value = useMemo(() => ({ me, error, reload, link, unlink }), [me, error, reload, link, unlink])
  return <DeviceContext.Provider value={value}>{children}</DeviceContext.Provider>
}
