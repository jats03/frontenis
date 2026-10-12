import type {
  CompetitionDetail, CompetitionSummary, CreateCompetition, GroupStandings, MatchDto,
  PairDto, PairingPreview, Player, Position,
} from './types'

export const BASE = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5080'
const TOKEN_KEY = 'frontenis.token'

export const tokenStore = {
  get: () => { try { return localStorage.getItem(TOKEN_KEY) } catch { return null } },
  set: (t: string) => { try { localStorage.setItem(TOKEN_KEY, t) } catch { /* sin almacenamiento */ } },
  clear: () => { try { localStorage.removeItem(TOKEN_KEY) } catch { /* sin almacenamiento */ } },
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) { super(message); this.status = status }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const token = tokenStore.get()
  if (token) headers.Authorization = `Bearer ${token}`

  // Si el servidor está dormido (Render) o la BD se está reanudando, las lecturas se reintentan
  // y, si tardan, se avisa al usuario (ver ApiStatus).
  window.dispatchEvent(new Event('frontenis:request-start'))
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
    return data as T
  } finally {
    clearTimeout(slowTimer)
    window.dispatchEvent(new Event('frontenis:request-end'))
    if (slowFired) window.dispatchEvent(new Event('frontenis:slow-end'))
  }
}

export const api = {
  login: (username: string, password: string) =>
    request<{ token: string; username: string; role: string }>('POST', '/api/auth/login', { username, password }),

  players: {
    list: () => request<Player[]>('GET', '/api/players'),
    create: (name: string, position: Position, groupNumber: number | null = null) =>
      request<Player>('POST', '/api/players', { name, position, groupNumber }),
    update: (id: number, name: string, position: Position, groupNumber: number | null = null) =>
      request<Player>('PUT', `/api/players/${id}`, { name, position, groupNumber }),
    remove: (id: number) => request<void>('DELETE', `/api/players/${id}`),
    pairingPreview: (playerIds: number[]) =>
      request<PairingPreview>('POST', '/api/players/pairing-preview', { playerIds }),
  },

  competitions: {
    list: () => request<CompetitionSummary[]>('GET', '/api/competitions'),
    get: (id: number) => request<CompetitionDetail>('GET', `/api/competitions/${id}`),
    standings: (id: number) => request<GroupStandings[]>('GET', `/api/competitions/${id}/standings`),
    create: (c: CreateCompetition) => request<CompetitionSummary>('POST', '/api/competitions', c),
    rename: (id: number, name: string) => request<CompetitionSummary>('PUT', `/api/competitions/${id}/name`, { name }),
    remove: (id: number) => request<void>('DELETE', `/api/competitions/${id}`),
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

  matches: {
    setResult: (id: number, homeScore: number, awayScore: number) =>
      request<MatchDto>('PUT', `/api/matches/${id}/result`, { homeScore, awayScore }),
    clearResult: (id: number) => request<MatchDto>('DELETE', `/api/matches/${id}/result`),
  },
}
