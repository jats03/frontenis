import { useCallback, useEffect, useRef, useState } from 'react'
import { responseCache } from './api/client'

/** Tiempo mínimo que se mantiene en pantalla un aviso de carga ya visible, para que no parpadee. */
export const MIN_LOADER_MS = 1000

/** Espera lo que falte hasta cumplir el tiempo mínimo del aviso de carga mostrado desde <paramref name="startedAt"/>. */
export function holdLoader(startedAt: number): Promise<void> {
  const left = MIN_LOADER_MS - (Date.now() - startedAt)
  return left > 0 ? new Promise(resolve => setTimeout(resolve, left)) : Promise.resolve()
}

/** Título de la pestaña por página (los lectores de pantalla lo anuncian al navegar). */
export function useDocumentTitle(title: string) {
  useEffect(() => { document.title = `${title} · Club Frontenis Ciudad de Jaén` }, [title])
}

/**
 * Carga datos asíncronos con estado de carga/error y recarga manual.
 * Con `cacheKey` (la ruta de la API) arranca con la última copia local, si existe, y la refresca por detrás.
 * Si la carga se hace sin datos que mostrar (aviso «Cargando…» visible), el resultado no se aplica antes de
 * `MIN_LOADER_MS`, para que las raquetas no parpadeen en cargas muy rápidas.
 * Solo se aplica la respuesta de la última carga lanzada: una respuesta lenta de una pantalla anterior
 * (otro `id`) o que llega con el componente ya desmontado se descarta.
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[], cacheKey?: string) {
  const [data, setData] = useState<T | null>(() => (cacheKey ? responseCache.peek<T>(cacheKey)?.data ?? null : null))
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const hasData = useRef(data !== null)
  useEffect(() => { hasData.current = data !== null }, [data])
  const lastRun = useRef(0)
  const mounted = useRef(false)
  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const run = useCallback(fn, deps)

  const reload = useCallback(async () => {
    const showsLoader = !hasData.current
    const startedAt = Date.now()
    const runId = ++lastRun.current
    // Una respuesta es obsoleta si ya se lanzó otra carga después o si el componente se desmontó.
    const stale = () => runId !== lastRun.current || !mounted.current
    setLoading(true)
    let outcome: { value: T } | { message: string }
    try { outcome = { value: await run() } }
    catch (e) { outcome = { message: e instanceof Error ? e.message : 'Error desconocido' } }
    if (showsLoader) await holdLoader(startedAt)
    if (stale()) {
      if (import.meta.env.DEV) console.debug('useAsync: respuesta obsoleta descartada', { cacheKey })
      return
    }
    if ('value' in outcome) { setData(outcome.value); setError(null) }
    else setError(outcome.message)
    setLoading(false)
  }, [run, cacheKey])

  useEffect(() => { void reload() }, [reload])
  return { data, setData, error, loading, reload }
}
