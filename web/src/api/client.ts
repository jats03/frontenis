import type {
  CompetitionDetail, CompetitionSummary, CreateCompetition, GroupStandings, MatchDto,
  PairDto, PairingPreview, Player, PlayerHistory, PlayerHistoryRow, Position, SetScore,
  HistoryFilter, NewTrainingMatch, TrainingDetail, TrainingSuggestion, TrainingSummary,
  AuditPage, AuditQuery, PendingResult, RejectedResult, DeviceLink, DeviceMe, PlayerDelegateRow, PlayerRef, SaveSlot, SaveSlotTemplate, Slot, SlotTemplate,
} from './types'
import { deviceHeaders, deviceToken, hasDeviceToken } from '../device'

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

async function request<T>(method: string, path: string, body?: unknown, extraHeaders?: Record<string, string>): Promise<T> {
  const headers: Record<string, string> = { ...extraHeaders }
  // Si este navegador ya tiene identificador, viaja siempre: el servidor anota quién hace cada cosa en su registro.
  if (!headers['X-Device-Token'] && hasDeviceToken()) headers['X-Device-Token'] = deviceToken()
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const token = tokenStore.get()
  if (token) headers.Authorization = `Bearer ${token}`

  // Si el servidor está dormido (Render) o la BD se está reanudando, las lecturas se reintentan
  // y mientras tanto se muestra el aviso «Cargando…» (ver LoadingSpinner y ApiStatus).
  // Una lectura con copia local es "silenciosa": no tapa la pantalla con el spinner porque ya hay datos que mostrar.
  const silent = method === 'GET' && responseCache.peek(path) !== null
  if (!silent) window.dispatchEvent(new Event('frontenis:request-start'))
  const attempts = method === 'GET' ? 4 : 1
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
      // Un 401 por un dispositivo sin vincular no significa que la sesión del administrador haya caducado.
      if (res.status === 401 && token && !extraHeaders) window.dispatchEvent(new Event('frontenis:unauthorized'))
      if (res.status === 429) throw new ApiError(429, 'Demasiadas peticiones seguidas. Espera un minuto e inténtalo de nuevo.')
      throw new ApiError(res.status, data?.detail ?? data?.title ?? `Error ${res.status}`)
    }
    if (method === 'GET' && data !== undefined) responseCache.save(path, data)
    return data as T
  } finally {
    if (!silent) window.dispatchEvent(new Event('frontenis:request-end'))
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
    rejectedResults: (id: number) => request<RejectedResult[]>('GET', `/api/competitions/${id}/rejected-results`),
    pendingResults: (id: number) => request<PendingResult[]>('GET', `/api/competitions/${id}/pending-results`),
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
    addRotatingPlayer: (id: number, playerId: number) =>
      request<CompetitionDetail>('POST', `/api/competitions/${id}/rotating/players`, { playerId }),
    addMatch: (id: number, groupId: number, m: { round: number; home1Id: number; home2Id: number; away1Id: number; away2Id: number }) =>
      request<CompetitionDetail>('POST', `/api/competitions/${id}/groups/${groupId}/matches`, m),
    swapRounds: (id: number, groupId: number, roundA: number, roundB: number) =>
      request<CompetitionDetail>('PUT', `/api/competitions/${id}/groups/${groupId}/rounds/swap`, { roundA, roundB }),
    removeRound: (id: number, groupId: number, round: number) =>
      request<CompetitionDetail>('DELETE', `/api/competitions/${id}/groups/${groupId}/rounds/${round}`),
    nextLeague: (id: number) => request<CompetitionSummary>('POST', `/api/competitions/${id}/next-league`),
    playoffs: (id: number) => request<CompetitionDetail>('POST', `/api/competitions/${id}/playoffs`),
  },

  trainings: {
    list: () => request<TrainingSummary[]>('GET', '/api/trainings'),
    get: (id: number) => request<TrainingDetail>('GET', `/api/trainings/${id}`),
    create: async (name: string, date: string, pointsPerWin: number, pointsPerLoss: number) => {
      const t = await request<TrainingSummary>('POST', '/api/trainings', { name, date, pointsPerWin, pointsPerLoss })
      responseCache.remove('/api/trainings')
      return t
    },
    remove: async (id: number) => {
      await request<void>('DELETE', `/api/trainings/${id}`)
      responseCache.remove('/api/trainings'); responseCache.remove(`/api/trainings/${id}`)
    },
    rename: async (id: number, name: string) => {
      const t = await request<TrainingSummary>('PUT', `/api/trainings/${id}/name`, { name })
      responseCache.remove('/api/trainings'); responseCache.remove(`/api/trainings/${id}`)
      return t
    },
    addMatch: async (id: number, m: NewTrainingMatch) => {
      const d = await request<TrainingDetail>('POST', `/api/trainings/${id}/matches`, m)
      responseCache.remove('/api/trainings')
      return d
    },
    removeMatch: async (id: number, matchId: number) => {
      const d = await request<TrainingDetail>('DELETE', `/api/trainings/${id}/matches/${matchId}`)
      responseCache.remove('/api/trainings')
      return d
    },
    suggest: (id: number, playerIds: number[], count = 5) =>
      request<TrainingSuggestion[]>('POST', `/api/trainings/${id}/suggestions`, { playerIds, count }),
  },

  slots: {
    /** Sin fechas: esta semana y la siguiente. */
    list: (from?: string, to?: string) => {
      const q = [from && `from=${from}`, to && `to=${to}`].filter(Boolean).join('&')
      return request<Slot[]>('GET', `/api/slots${q ? `?${q}` : ''}`)
    },
    create: (s: SaveSlot) => request<Slot>('POST', '/api/slots', s),
    update: (id: number, s: SaveSlot) => request<Slot>('PUT', `/api/slots/${id}`, s),
    cancel: (id: number) => request<Slot>('POST', `/api/slots/${id}/cancel`),
    restore: (id: number) => request<Slot>('POST', `/api/slots/${id}/restore`),
    openNow: (id: number) => request<Slot>('POST', `/api/slots/${id}/open`),
    closeSignUps: (id: number) => request<Slot>('POST', `/api/slots/${id}/close`),
    remove: (id: number) => request<void>('DELETE', `/api/slots/${id}`),
    generate: (from: string, weeks: number) =>
      request<{ created: number }>('POST', '/api/slots/generate', { from, weeks }),
    openWeek: (weekStart: string) => request<{ opened: number }>('POST', '/api/slots/open-week', { weekStart }),
    closeWeek: (weekStart: string) => request<{ closed: number }>('POST', '/api/slots/close-week', { weekStart }),
    /** Administrador: apunta y quita a cualquier jugador. */
    signUp: (slotId: number, playerId: number) =>
      request<Slot>('POST', `/api/slots/${slotId}/attendees`, { playerId }),
    withdraw: (slotId: number, playerId: number) =>
      request<Slot>('DELETE', `/api/slots/${slotId}/attendees/${playerId}`),
    /** Jugador desde su dispositivo vinculado: solo a sí mismo y a sus apadrinados. */
    attend: (slotId: number, playerId: number) =>
      request<Slot>('POST', `/api/slots/${slotId}/attend`, { playerId }, deviceHeaders()),
    unattend: (slotId: number, playerId: number) =>
      request<Slot>('DELETE', `/api/slots/${slotId}/attend/${playerId}`, undefined, deviceHeaders()),
  },

  device: {
    /** `undefined` (204) si este dispositivo aún no está vinculado a ningún jugador. */
    me: () => request<DeviceMe | undefined>('GET', '/api/device/me', undefined, deviceHeaders()),
    availablePlayers: () => request<PlayerRef[]>('GET', '/api/device/available-players'),
    link: (playerId: number) => request<DeviceMe>('POST', '/api/device/link', { playerId }, deviceHeaders()),
    unlink: () => request<void>('DELETE', '/api/device/link', undefined, deviceHeaders()),
  },

  audit: {
    /** Registro de actividad (solo administrador). Las fechas son días locales (aaaa-mm-dd). */
    search: (q: AuditQuery, pageSize = 50) => {
      const p = new URLSearchParams({ page: String(q.page), pageSize: String(pageSize) })
      if (q.actor) p.set('actor', q.actor)
      if (q.text.trim()) p.set('text', q.text.trim())
      if (q.method) p.set('method', q.method)
      if (q.from) p.set('from', new Date(`${q.from}T00:00:00`).toISOString())
      if (q.to) { const end = new Date(`${q.to}T00:00:00`); end.setDate(end.getDate() + 1); p.set('to', end.toISOString()) }
      return request<AuditPage>('GET', `/api/audit?${p}`)
    },
  },

  deviceLinks: {
    list: () => request<DeviceLink[]>('GET', '/api/device-links'),
    release: (playerId: number) => request<void>('DELETE', `/api/device-links/${playerId}`),
  },

  playerDelegates: {
    list: () => request<PlayerDelegateRow[]>('GET', '/api/player-delegates'),
    add: (ownerPlayerId: number, delegatePlayerId: number) =>
      request<PlayerDelegateRow>('POST', '/api/player-delegates', { ownerPlayerId, delegatePlayerId }),
    remove: (ownerId: number, delegateId: number) => request<void>('DELETE', `/api/player-delegates/${ownerId}/${delegateId}`),
  },

  slotTemplates: {
    list: () => request<SlotTemplate[]>('GET', '/api/slot-templates'),
    create: (t: SaveSlotTemplate) => request<SlotTemplate>('POST', '/api/slot-templates', t),
    update: (id: number, t: SaveSlotTemplate) => request<SlotTemplate>('PUT', `/api/slot-templates/${id}`, t),
    remove: (id: number) => request<void>('DELETE', `/api/slot-templates/${id}`),
  },

  matches: {
    setResult: (id: number, sets: SetScore[]) =>
      request<MatchDto>('PUT', `/api/matches/${id}/result`, { sets }),
    moveToRound: (id: number, round: number) =>
      request<CompetitionDetail>('PUT', `/api/matches/${id}/round`, { round }),
    remove: (id: number) => request<CompetitionDetail>('DELETE', `/api/matches/${id}`),
    swapWith: (id: number, otherMatchId: number) =>
      request<CompetitionDetail>('PUT', `/api/matches/${id}/swap`, { otherMatchId }),
    clearResult: (id: number) => request<MatchDto>('DELETE', `/api/matches/${id}/result`),
    /** Un jugador identificado envía el resultado; queda pendiente de que lo apruebe el administrador. */
    submitResult: (id: number, sets: SetScore[]) =>
      request<PendingResult>('PUT', `/api/matches/${id}/submission`, { sets }),
  },

  matchSubmissions: {
    approve: (id: number) => request<MatchDto>('POST', `/api/match-submissions/${id}/approve`),
    reject: (id: number) => request<void>('DELETE', `/api/match-submissions/${id}`),
    deleteRejected: (id: number) => request<void>('DELETE', `/api/match-rejections/${id}`),
    restore: (id: number) => request<PendingResult>('POST', `/api/match-rejections/${id}/restore`),
  },
}
