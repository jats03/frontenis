import { createContext, useContext } from 'react'
import type { DeviceMe } from './api/types'

/**
 * Estado del vínculo de este dispositivo con su jugador (compartido por toda la app).
 * `me`: `undefined` mientras carga, `null` si no está vinculado.
 */
export interface DeviceState {
  me: DeviceMe | null | undefined
  error: string | null
  reload: () => Promise<void>
  link: (playerId: number) => Promise<void>
  unlink: () => Promise<void>
}

export const DeviceContext = createContext<DeviceState | null>(null)

export function useDevice(): DeviceState {
  const ctx = useContext(DeviceContext)
  if (!ctx) throw new Error('useDevice debe usarse dentro de <DeviceProvider>')
  return ctx
}
