import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, responseCache, tokenStore } from '../api/client'

interface AuthState {
  isAdmin: boolean
  username: string | null
  login: (u: string, p: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)
const USER_KEY = 'frontenis.user'

function readUser(): string | null {
  try { return tokenStore.get() ? localStorage.getItem(USER_KEY) : null } catch { return null }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [username, setUsername] = useState<string | null>(readUser)

  const logout = useCallback(() => {
    tokenStore.clear()
    responseCache.clear()
    try { localStorage.removeItem(USER_KEY) } catch { /* sin almacenamiento */ }
    setUsername(null)
  }, [])

  const login = useCallback(async (u: string, p: string) => {
    const res = await api.login(u, p)
    tokenStore.set(res.token)
    try { localStorage.setItem(USER_KEY, res.username) } catch { /* sin almacenamiento */ }
    setUsername(res.username)
  }, [])

  useEffect(() => {
    window.addEventListener('frontenis:unauthorized', logout)
    return () => window.removeEventListener('frontenis:unauthorized', logout)
  }, [logout])

  const value = useMemo(() => ({ isAdmin: username !== null, username, login, logout }), [username, login, logout])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}
