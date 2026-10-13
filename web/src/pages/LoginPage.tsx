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

  async function submit(e: FormEvent) {
    e.preventDefault()
    try { await login(username, password); navigate('/') }
    catch (err) { setError(err instanceof Error ? err.message : 'Error') }
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h1>Acceso administrador</h1>
      <label>Usuario<input value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" /></label>
      <label>Contraseña
        <input type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="current-password" />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit">Entrar</button>
    </form>
  )
}
