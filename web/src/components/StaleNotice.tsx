/** Aviso cuando se muestran datos guardados en el dispositivo mientras se confirman con el servidor. */
export default function StaleNotice({ savedAt, error }: { savedAt: number | null; error: string | null }) {
  if (savedAt === null) return null
  const minutes = Math.max(0, Math.round((Date.now() - savedAt) / 60000))
  const age = minutes < 1 ? 'hace un momento' : minutes < 60 ? `hace ${minutes} min` : minutes < 1440 ? `hace ${Math.round(minutes / 60)} h` : `hace ${Math.round(minutes / 1440)} d`
  return (
    <p className="stale-notice" role="status">
      Mostrando datos guardados ({age}). {error ? 'No se pudo actualizar todavía; se reintentará al volver a cargar la página.' : 'Actualizando…'}
    </p>
  )
}
