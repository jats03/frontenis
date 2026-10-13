import { useState, type FormEvent } from 'react'
import { notify } from '../alerts'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { useDocumentTitle } from '../hooks'

const MIN = 12

export default function AccountPage() {
  useDocumentTitle('Mi cuenta')
  const { isAdmin, username } = useAuth()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [repeat, setRepeat] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  if (!isAdmin) return <><h1>Mi cuenta</h1><p className="error">Necesitas iniciar sesión como administrador.</p></>

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (next.length < MIN) { setError(`La nueva contraseña debe tener al menos ${MIN} caracteres.`); return }
    if (next !== repeat) { setError('Las contraseñas nuevas no coinciden.'); return }
    setBusy(true)
    try {
      await api.auth.changePassword(current, next)
      setCurrent(''); setNext(''); setRepeat('')
      notify('Contraseña cambiada')
    } catch (err) { setError(err instanceof Error ? err.message : 'Error') }
    finally { setBusy(false) }
  }

  return (
    <form className="card form narrow" onSubmit={submit}>
      <h1>Mi cuenta</h1>
      <p className="muted">Usuario: <strong>{username}</strong></p>
      <h2>Cambiar contraseña</h2>
      <label>Contraseña actual
        <input type="password" value={current} onChange={e => setCurrent(e.target.value)} autoComplete="current-password" required maxLength={128} />
      </label>
      <label>Contraseña nueva (mínimo {MIN} caracteres)
        <input type="password" value={next} onChange={e => setNext(e.target.value)} autoComplete="new-password" required minLength={MIN} maxLength={128} />
      </label>
      <label>Repite la contraseña nueva
        <input type="password" value={repeat} onChange={e => setRepeat(e.target.value)} autoComplete="new-password" required maxLength={128} />
      </label>
      <p className="muted">Consejo: una frase larga y fácil de recordar es más segura que una palabra con símbolos. Usa una contraseña que no uses en ningún otro sitio.</p>
      {error && <p className="error" role="alert">{error}</p>}
      <button type="submit" disabled={busy}>Guardar contraseña</button>
    </form>
  )
}
