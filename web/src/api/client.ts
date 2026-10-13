import type {
  CompetitionDetail, CompetitionSummary, CreateCompetition, GroupStandings, MatchDto,
  PairDto, PairingPreview, Player, PlayerHistory, PlayerHistoryRow, Position, SetScore,
  HistoryFilter, NewTrainingMatch, TrainingDetail, TrainingSuggestion, TrainingSummary,
} from './types'

export const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5080'
const TOKEN_KEY = 'frontenis.token'

export const tokenStore = {
  get: () => { try { return localStorage.getItem(TOKEN_KEY) } catch { return null } },
  set: (t: string) => { try { localStorage.setItem(TOKEN_KEY, t) } catch { /* sin almacenamiento */ } },
  clear: () => { try { localStorage.removeItem(TOKEN_KEY) } catch { /* sin almacenamiento */ } },
}

/**
 * Copia local de la última respuesta correcta de cada lectura (GET). Permite pintar al instante lo último
 * conocido mientras el servidor o la base de datos despiertan, y se refresca por detrás.
 */
const CACHE_PREFIX = 'frontenis.cache:'
export interface CachedEntry<T> { data: T; savedAt: number }
export const responseCache = {
  peek<T>(path: string): CachedEntry<T> | null {
    try {
      const raw = localStorage.getItem(CACHE_PREFIX + path)
      if (!raw) return null
      const parsed = JSON.parse(raw) as CachedEntry<T>
      return typeof parsed?.savedAt === 'number' && parsed.data !== undefined ? parsed : null
    } catch { return null }
  },
  save<T>(path: string, data: T) {
    try { localStorage.setItem(CACHE_PREFIX + path, JSON.stringify({ data, savedAt: Date.now() })) }
    catch { /* sin almacenamiento o cuota llena: no pasa nada */ }
  },
  remove(path: string) {
    try { localStorage.removeItem(CACHE_PREFIX + path) } catch { /* sin almacenamiento */ }
  },
  /** Borra todas las copias (se usa al cerrar sesión). */
  clear() {
    try {
      Object.keys(localStorage).filter(k => k.startsWith(CACHE_PREFIX)).forEach(k => localStorage.removeItem(k))
    } catch { /* sin almacenamiento */ }
  },
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) { super(message); this.status = status }
}

function historyQuery(f: HistoryFilter): string {
  const q = [...f.competitionIds.map(i => `competitionIds=${i}`), ...f.trainingIds.map(i => `trainingIds=${i}`)]
  return q.length ? `?${q.join('&')}` : ''
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const token = tokenStore.get()
  if (token) headers.Authorization = `Bearer ${token}`

  // Si el servidor está dormido (Render) o la BD se está reanudando, las lecturas se reintentan
  // y, si tardan, se avisa al usuario (ver ApiStatus).
  // Una lectura con copia local es "silenciosa": no tapa la pantalla con el spinner porque ya hay datos que mostrar.
  const silent = method === 'GET' && responseCache.peek(path) !== null
  if (!silent) window.dispatchEvent(new Event('frontenis:request-start'))
  const SLOW_MS = 4000
  const attempts = method === 'GET' ? 4 : 1
  let slowFired = false
  const slowTimer = setTimeout(() => { slowFired = true; window.dispatchEvent(new Event('frontenis:slow-start')) }, SLOW_MS)
  try {
    let res: Response | undefined
    for (let i = 0; i < attempts; i++) {
      const last = i === attempts - 1
      try {
        res = await fetch(`${BASE}${path}`, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) })
        if (last || ![502, 503, 504].includes(res.status)) break
      } catch {
        if (last) throw new ApiError(0, 'No se pudo conectar con el servidor.')
      }
      await new Promise(r => setTimeout(r, 3000))
    }
    if (!res) throw new ApiError(0, 'No se pudo conectar con el servidor.')
    if (res.status === 204) return undefined as T
    const text = await res.text()
    let data: { detail?: string; title?: string } | undefined
    try { data = text ? JSON.parse(text) : undefined } catch { data = undefined }
    if (!res.ok) {
      if (res.status === 401 && token) window.dispatchEvent(new Event('frontenis:unauthorized'))
      throw new ApiError(res.status, data?.detail ?? data?.title ?? `Error ${res.status}`)
    }
    if (method === 'GET' && data !== undefined) responseCache.save(path, data)
    return data as T
  } finally {
    clearTimeout(slowTimer)
    if (!silent) window.dispatchEvent(new Event('frontenis:request-end'))
    if (slowFired) window.dispatchEvent(new Event('frontenis:slow-end'))
  }
}

export const api = {
  login: (username: string, password: string) =>
    request<{ token: string; username: string; role: string }>('POST', '/api/auth/login', { username, password }),

  auth: {
    changePassword: (currentPassword: string, newPassword: string) =>
      request<void>('POST', '/api/auth/change-password', { currentPassword, newPassword }),
  },

  players: {
    list: () => request<Player[]>('GET', '/api/players'),
    create: (name: string, position: Position, groupNumber: number | null = null) =>
      request<Player>('POST', '/api/players', { name, position, groupNumber }),
    update: (id: number, name: string, position: Position, groupNumber: number | null = null) =>
      request<Player>('PUT', `/api/players/${id}`, { name, position, groupNumber }),
    remove: (id: number) => request<void>('DELETE', `/api/players/${id}`),
    history: (f: HistoryFilter) => request<PlayerHistoryRow[]>('GET', `/api/players/history${historyQuery(f)}`),
    playerHistory: (id: number, f: HistoryFilter) =>
      request<PlayerHistory>('GET', `/api/players/${id}/history${historyQuery(f)}`),
    pairingPreview: (playerIds: number[]) =>
      request<PairingPreview>('POST', '/api/players/pairing-preview', { playerIds }),
  },

  competitions: {
    list: () => request<CompetitionSummary[]>('GET', '/api/competitions'),
    get: (id: number) => request<CompetitionDetail>('GET', `/api/competitions/${id}`),
    standings: (id: number) => request<GroupStandings[]>('GET', `/api/competitions/${id}/standings`),
    create: (c: CreateCompetition) => request<CompetitionSummary>('POST', '/api/competitions', c),
    setSetsPerMatch: (id: number, setsPerMatch: number) =>
      request<CompetitionSummary>('PUT', `/api/competitions/${id}/sets-per-match`, { setsPerMatch }),
    rename: (id: number, name: string) => request<CompetitionSummary>('PUT', `/api/competitions/${id}/name`, { name }),
    remove: async (id: number) => {
      await request<void>('DELETE', `/api/competitions/${id}`)
      responseCache.remove(`/api/competitions/${id}`)
      responseCache.remove(`/api/competitions/${id}/standings`)
    },
    registerRandom: (id: number, playerIds: number[]) =>
      request<PairDto[]>('POST', `/api/competitions/${id}/pairs/random`, { playerIds }),
    registerManual: (id: number, player1Id: number, player2Id: number) =>
      request<PairDto>('POST', `/api/competitions/${id}/pairs`, { player1Id, player2Id }),
    draw: (id: number) => request<CompetitionDetail>('POST', `/api/competitions/${id}/draw`),
    drawManual: (id: number, groups: number[][]) =>
      request<CompetitionDetail>('POST', `/api/competitions/${id}/draw-manual`, { groups }),
    drawRotatingManual: (id: number, groups: number[][]) =>
      request<CompetitionDetail>('POST', `/api/competitions/${id}/draw-rotating-manual`, { groups }),
    drawRotating: (id: number, playerIds: number[]) =>
      request<CompetitionDetail>('POST', `/api/competitions/${id}/draw-rotating`, { playerIds }),
    nextLeague: (id: number) => request<CompetitionSummary>('POST', `/api/competitions/${id}/next-league`),
    playoffs: (id: number) => request<CompetitionDetail>('POST', `/api/competitions/${id}/playoffs`),
  },

  trainings: {
    list: () => request<TrainingSummary[]>('GET', '/api/trainings'),
    get: (id: number) => request<TrainingDetail>('GET', `/api/trainings/${id}`),
    create: (name: string, date: string, pointsPerWin: number, pointsPerLoss: number) =>
      request<TrainingSummary>('POST', '/api/trainings', { name, date, pointsPerWin, pointsPerLoss }),
    remove: (id: number) => request<void>('DELETE', `/api/trainings/${id}`),
    addMatch: (id: number, m: NewTrainingMatch) => request<TrainingDetail>('POST', `/api/trainings/${id}/matches`, m),
    removeMatch: (id: number, matchId: number) => request<TrainingDetail>('DELETE', `/api/trainings/${id}/matches/${matchId}`),
    suggest: (id: number, playerIds: number[], count = 5) =>
      request<TrainingSuggestion[]>('POST', `/api/trainings/${id}/suggestions`, { playerIds, count }),
  },

  matches: {
    setResult: (id: number, sets: SetScore[]) =>
      request<MatchDto>('PUT', `/api/matches/${id}/result`, { sets }),
    clearResult: (id: number) => request<MatchDto>('DELETE', `/api/matches/${id}/result`),
  },
}
