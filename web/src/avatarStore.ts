import { useSyncExternalStore } from 'react'
import { responseCache } from './api/client'

/**
 * Avatar elegido por cada jugador (1 a 36; ausente = el de siempre, que sale de su identificador). Se llena con la lista
 * de jugadores, que ya incluye el avatar, y se actualiza al instante cuando alguien cambia el suyo, para que todas las
 * pantallas (saludo, carta, listas, clasificaciones) lo muestren sin recargar.
 */
const seeds = new Map<number, number>()
const listeners = new Set<() => void>()
let version = 0

function emit() { version++; listeners.forEach(l => l()) }

/** Sustituye todos los avatares conocidos por los de la lista de jugadores. */
export function loadAvatarSeeds(players: { id: number; avatarSeed?: number | null }[]) {
  seeds.clear()
  for (const p of players) if (p.avatarSeed) seeds.set(p.id, p.avatarSeed)
  emit()
}

/** Cambia el avatar de un jugador (null = el de siempre). */
export function setAvatarSeed(playerId: number, seed: number | null) {
  if (seed) seeds.set(playerId, seed); else seeds.delete(playerId)
  // La copia local de la lista de jugadores ya no vale: se vuelve a pedir la próxima vez.
  responseCache.remove('/api/players')
  emit()
}

// Lo último conocido, para no pintar primero los de siempre y cambiarlos después.
try {
  const cached = responseCache.peek<{ id: number; avatarSeed?: number | null }[]>('/api/players')
  if (cached) loadAvatarSeeds(cached.data)
} catch { /* sin almacenamiento */ }

/** Avatar elegido por ese jugador, o null si usa el de siempre. */
export function useAvatarSeed(playerId: number): number | null {
  useSyncExternalStore(cb => { listeners.add(cb); return () => { listeners.delete(cb) } }, () => version)
  return seeds.get(playerId) ?? null
}
