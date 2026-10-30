import { useState } from 'react'
import { notify } from '../alerts'

interface Props {
  label: string
  /** Texto accesible más descriptivo que la etiqueta visible. */
  ariaLabel?: string
  /** Crea la imagen y la entrega (menú de compartir en el móvil, descarga en el ordenador). */
  onShare: () => Promise<void>
}

/** Genera la imagen de los apuntados para que la persona la mande por WhatsApp al grupo que quiera. */
export default function ShareImageButton({ label, ariaLabel, onShare }: Props) {
  const [busy, setBusy] = useState(false)
  async function click() {
    setBusy(true)
    try { await onShare() } catch { notify('No se pudo crear la imagen', 'info') }
    finally { setBusy(false) }
  }
  return (
    <button type="button" className="secondary share-reminder" disabled={busy} aria-label={ariaLabel ?? label} onClick={() => void click()}>
      {busy ? 'Creando la imagen…' : label}
    </button>
  )
}
