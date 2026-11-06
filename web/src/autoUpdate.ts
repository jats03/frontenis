/**
 * Actualización automática de la app (PWA). Cuando hay una versión nueva desplegada, el service worker nuevo toma el
 * control y la página se recarga sola, para que nadie se quede con una versión antigua o rota.
 * Al abrir la app se enseña primero la versión guardada y la nueva llega unos segundos después: si llega nada más
 * entrar (primeros segundos de la visita) se recarga enseguida, aunque la persona ya haya tocado algo. En una visita
 * ya larga no se la interrumpe: la recarga se aplica al salir de la app (pestaña en segundo plano).
 * Nunca recarga más de una vez por minuto, para evitar bucles.
 */
const RELOAD_KEY = 'frontenis.auto-reload'
const MIN_GAP_MS = 60_000
const CHECK_EVERY_MS = 60 * 60 * 1000
/** «Nada más entrar»: durante este tiempo desde que se abre la app, una versión nueva se aplica al instante. */
export const ENTRY_WINDOW_MS = 30_000
const openedAt = Date.now()

function reloadedRecently(): boolean {
  const last = Number(sessionStorage.getItem(RELOAD_KEY))
  return last > 0 && Date.now() - last < MIN_GAP_MS
}

/** Recarga la página una vez (máximo una por minuto). Devuelve false si no la ha hecho. */
export function reloadOnce(reason: string): boolean {
  try {
    if (reloadedRecently()) {
      console.warn(`Recarga automática omitida (${reason}): ya se recargó hace menos de un minuto`)
      return false
    }
    sessionStorage.setItem(RELOAD_KEY, String(Date.now()))
  } catch {
    // Sin sessionStorage no se puede garantizar que no haya bucle: no se recarga sola.
    console.warn(`Recarga automática omitida (${reason}): sin almacenamiento de sesión`)
    return false
  }
  console.info(`Recargando para aplicar la versión nueva (${reason})`)
  location.reload()
  return true
}

let pendingReason: string | null = null

function applyUpdate(reason: string) {
  // Nada más entrar (o con la pestaña en segundo plano) se recarga ya: hay poco o nada que perder y así nadie se queda
  // con la versión antigua. Pasados esos primeros segundos, se espera a que la persona salga de la app.
  if (document.hidden || Date.now() - openedAt < ENTRY_WINDOW_MS) {
    reloadOnce(reason)
    return
  }
  pendingReason = reason
  console.info(`Versión nueva lista (${reason}): se aplicará al salir de la app`)
}

export function startAutoUpdate() {
  if (!('serviceWorker' in navigator)) return

  // Solo cuenta el cambio si la página ya estaba controlada por un service worker (no en la primera instalación).
  const hadController = navigator.serviceWorker.controller !== null
  navigator.serviceWorker.addEventListener('controllerchange', () => {
    if (hadController) applyUpdate('service worker nuevo')
  })

  // El service worker nuevo pregunta a cada pestaña si se encarga ella de actualizarse; esta versión sí (con su espera
  // a que la persona salga de la app) y se lo confirma para que el service worker no la recargue por su cuenta.
  navigator.serviceWorker.addEventListener('message', event => {
    if (event.data?.type !== 'frontenis-update') return
    event.ports[0]?.postMessage('ok')
    applyUpdate('aviso del service worker')
  })

  const checkForUpdate = () => {
    navigator.serviceWorker.getRegistration().then(registration => registration?.update()).catch(() => undefined)
  }
  document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
      if (pendingReason) {
        const reason = pendingReason
        pendingReason = null
        reloadOnce(reason)
      }
    } else {
      checkForUpdate() // al volver a la app se comprueba si hay versión nueva
    }
  })
  window.setInterval(checkForUpdate, CHECK_EVERY_MS)
  // Nada más abrir la app se pregunta si hay versión nueva (sin esperar a volver a ella ni a la hora siguiente).
  navigator.serviceWorker.ready.then(registration => registration.update()).catch(() => undefined)

  // Vite avisa cuando no se puede descargar un trozo de código (p. ej. porque ya no existe tras un despliegue).
  // Solo se cancela el error si de verdad se recarga; si no (ya se recargó hace poco), el error sigue su camino hasta
  // la pantalla de fallo con el botón Reintentar en lugar de quedarse en silencio.
  window.addEventListener('vite:preloadError', event => {
    if (reloadOnce('trozo de código ausente')) event.preventDefault()
  })
}
