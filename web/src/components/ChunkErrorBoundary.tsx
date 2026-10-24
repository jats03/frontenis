import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props { children: ReactNode; resetKey: string }
interface State { failed: boolean; key: string }

/** Si no se puede descargar el código de una pantalla (p. ej. sin cobertura), avisa y deja reintentar. */
export default class ChunkErrorBoundary extends Component<Props, State> {
  state: State = { failed: false, key: this.props.resetKey }

  static getDerivedStateFromError(): Partial<State> { return { failed: true } }

  // Al cambiar de ruta se vuelve a intentar con normalidad.
  static getDerivedStateFromProps(props: Props, state: State): Partial<State> | null {
    return props.resetKey !== state.key ? { failed: false, key: props.resetKey } : null
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('No se pudo cargar la pantalla', error, info.componentStack)
  }

  render() {
    if (!this.state.failed) return this.props.children
    return (
      <div role="alert">
        <p className="error">No se pudo cargar esta pantalla. Revisa la conexión e inténtalo de nuevo.</p>
        <button type="button" onClick={() => this.setState({ failed: false })}>Reintentar</button>
      </div>
    )
  }
}
