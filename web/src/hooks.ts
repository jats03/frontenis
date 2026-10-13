import { useCallback, useEffect, useState } from 'react'
import { responseCache } from './api/client'

/** Título de la pestaña por página (los lectores de pantalla lo anuncian al navegar). */
export function useDocumentTitle(title: string) {
  useEffect(() => { document.title = `${title} · Frontenis` }, [title])
}

/**
 * Carga datos asíncronos con estado de carga/error y recarga manual.
 * Con `cacheKey` (la ruta de la API) arranca con la última copia local, si existe, y la refresca por detrás:
 * `savedAt` indica que lo mostrado es una copia pendiente de confirmar.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[], cacheKey?: string) {
  const [data, setData] = useState<T | null>(() => (cacheKey ? responseCache.peek<T>(cacheKey)?.data ?? null : null))
  const [savedAt, setSavedAt] = useState<number | null>(() => (cacheKey ? responseCache.peek<T>(cacheKey)?.savedAt ?? null : null))
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const run = useCallback(fn, deps)

  const reload = useCallback(async () => {
    setLoading(true)
    try { setData(await run()); setSavedAt(null); setError(null) }
    catch (e) { setError(e instanceof Error ? e.message : 'Error desconocido') }
    finally { setLoading(false) }
  }, [run])

  useEffect(() => { void reload() }, [reload])
  return { data, setData, error, loading, reload, savedAt }
}
