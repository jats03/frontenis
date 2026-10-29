// Lo carga el service worker generado por vite-plugin-pwa (workbox.importScripts).
//
// Cuando este service worker sustituye a otro anterior, las pestañas que ya estaban abiertas siguen mostrando la versión
// antigua hasta que alguien recarga. Aquí se las recarga solas, incluso si su código es tan antiguo que no sabe
// actualizarse: primero se avisa a cada pestaña por si ella misma gestiona la actualización (la versión actual espera a
// que la persona salga de la app si la está usando) y, si no contesta, se recarga desde aquí.
//
// En la primera instalación no hay service worker anterior y no se recarga nada.

let replacesAnother = false

self.addEventListener('install', () => {
  // Con una actualización, el service worker anterior sigue siendo el activo mientras este se instala.
  replacesAnother = self.registration.active !== null
})

/** Pregunta a la pestaña si se encarga ella de actualizarse; sin respuesta en poco tiempo, se entiende que no. */
function handledByPage(client) {
  return new Promise(resolve => {
    const channel = new MessageChannel()
    const timer = setTimeout(() => resolve(false), 500)
    channel.port1.onmessage = () => { clearTimeout(timer); resolve(true) }
    client.postMessage({ type: 'frontenis-update' }, [channel.port2])
  })
}

self.addEventListener('activate', event => {
  if (!replacesAnother) return
  event.waitUntil((async () => {
    // Una pestaña solo se puede recargar desde aquí si este service worker la controla.
    await self.clients.claim()
    const windows = await self.clients.matchAll({ type: 'window' })
    let reloaded = 0
    await Promise.all(windows.map(async client => {
      if (await handledByPage(client)) return
      try { await client.navigate(client.url); reloaded++ } catch { /* la pestaña se cerró o no se puede recargar */ }
    }))
    console.info(`Service worker actualizado: ${windows.length} pestaña(s) abierta(s), ${reloaded} recargada(s) desde aquí`)
  })())
})
