import { useDocumentTitle } from '../hooks'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export default function LoginPage() {
  useDocumentTitle('Acceso de administrador')
  const { login } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (busy) return
    setBusy(true); setError(null)
    try { await login(username.trim(), password); setPassword(''); navigate('/') }
    catch (err) { setPassword(''); setError(err instanceof Error ? err.message : 'Error') }
    finally { setBusy(false) }
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h1>Acceso administrador</h1>
      <label>Usuario
        <input value={username} onChange={e => setUsername(e.target.value)} autoComplete="username"
          autoCapitalize="none" spellCheck={false} required maxLength={100} />
      </label>
      <label>Contraseña
        <input type="password" value={password} onChange={e => setPassword(e.target.value)}
          autoComplete="current-password" required maxLength={128} />
      </label>
      {error && <p className="error" role="alert">{error}</p>}
      <button type="submit" disabled={busy}>{busy ? 'Entrando…' : 'Entrar'}</button>
    </form>
  )
}
