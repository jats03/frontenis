// Simulación de la parte de la página (autoUpdate.ts): reloj, service worker, pestaña y sessionStorage falsos.
// Los casos 1 y 2 incluyen que la persona ya ha tocado la pantalla (pointerdown) antes de que llegue la versión nueva.
// Uso: node --experimental-strip-types simula-recarga.mjs <ruta a src/autoUpdate.ts>
import { pathToFileURL } from 'node:url'

const file = process.argv[2]
const realNow = Date.now.bind(Date)
let n = 0
const results = []

async function scenario(name, run) {
  n++
  const clock = { t: 1_000_000 }
  Date.now = () => clock.t
  const listeners = {}            // eventos de la ventana, del documento y del service worker
  const on = (target) => (type, fn) => { (listeners[target + ':' + type] ??= []).push(fn) }
  const emit = (target, type, ev) => (listeners[target + ':' + type] ?? []).forEach(fn => fn(ev))
  const store = new Map()
  const state = { reloads: 0, updates: 0, hidden: false, logs: [] }
  const sw = { controller: run.firstInstall ? null : {}, addEventListener: on('sw'), updates: 0 }
  const registration = { update: async () => { state.updates++ } }
  sw.ready = Promise.resolve(registration)
  sw.getRegistration = async () => registration
  globalThis.window = { addEventListener: on('win'), setInterval: () => 0 }
  Object.defineProperty(globalThis, 'document', { configurable: true, value: { get hidden() { return state.hidden }, addEventListener: on('doc') } })
  Object.defineProperty(globalThis, 'navigator', { configurable: true, value: { serviceWorker: sw } })
  globalThis.sessionStorage = { getItem: k => store.get(k) ?? null, setItem: (k, v) => store.set(k, String(v)) }
  globalThis.location = { reload: () => { state.reloads++ } }
  const logs = []
  const orig = { info: console.info, warn: console.warn }
  console.info = (...a) => logs.push(a.join(' '))
  console.warn = (...a) => logs.push('(aviso) ' + a.join(' '))

  const mod = await import(pathToFileURL(file).href + '?s=' + n)
  mod.startAutoUpdate()
  await Promise.resolve(); await Promise.resolve()   // deja pasar `ready.then(update)`
  const ctx = {
    advance: ms => { clock.t += ms }, state, emit,
    swChanged: () => emit('sw', 'controllerchange', {}),
    swMessage: () => { const port = { got: null, postMessage: m => { port.got = m } }; emit('sw', 'message', { data: { type: 'frontenis-update' }, ports: [port] }); return port },
    hide: () => { state.hidden = true; emit('doc', 'visibilitychange', {}) },
    show: () => { state.hidden = false; emit('doc', 'visibilitychange', {}) },
    window: mod,
  }
  await run.go(ctx)
  console.info = orig.info; console.warn = orig.warn
  results.push({ name, ...run.check(ctx), logs })
}

const ok = (cond) => (cond ? 'OK' : 'FALLA')

await scenario('1. Se abre la app y ya se ha tocado algo; a los 5 s llega la versión nueva (service worker nuevo)', {
  go: async c => { c.emit('win', 'pointerdown', {}); c.advance(5_000); c.swChanged() },
  check: c => ({ esperado: 'se recarga ya (1)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 1) }),
})
await scenario('2. Igual, pero el aviso llega como mensaje del service worker nuevo (a los 8 s)', {
  go: async c => { c.emit('win', 'pointerdown', {}); c.advance(8_000); c.port = c.swMessage() },
  check: c => ({ esperado: 'responde "ok" y se recarga (1)', obtenido: `respuesta=${c.port.got}, recargas=${c.state.reloads}`, veredicto: ok(c.port.got === 'ok' && c.state.reloads === 1) }),
})
await scenario('3. Visita ya larga (10 min) con la pestaña visible: llega la versión nueva', {
  go: async c => { c.advance(600_000); c.swChanged() },
  check: c => ({ esperado: 'no se interrumpe (0 recargas)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 0) }),
})
await scenario('4. Igual que la 3 y después la persona sale de la app (pestaña en segundo plano)', {
  go: async c => { c.advance(600_000); c.swChanged(); c.hide() },
  check: c => ({ esperado: 'se recarga al salir (1)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 1) }),
})
await scenario('5. Visita larga con la pestaña ya en segundo plano: llega la versión nueva', {
  go: async c => { c.advance(600_000); c.state.hidden = true; c.swChanged() },
  check: c => ({ esperado: 'se recarga ya (1)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 1) }),
})
await scenario('6. Dos versiones nuevas seguidas en menos de un minuto', {
  go: async c => { c.advance(5_000); c.swChanged(); c.advance(20_000); c.swChanged() },
  check: c => ({ esperado: 'solo una recarga (sin bucle)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 1) }),
})
await scenario('7. Primera instalación (la página no tenía service worker)', {
  firstInstall: true,
  go: async c => { c.advance(5_000); c.swChanged() },
  check: c => ({ esperado: 'no se recarga (0)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 0) }),
})
await scenario('8. Nada más abrir la app, sin esperar a nada', {
  go: async () => {},
  check: c => ({ esperado: 'se pregunta ya si hay versión nueva (1 comprobación)', obtenido: c.state.updates, veredicto: ok(c.state.updates === 1) }),
})
await scenario('9. Al volver a la app tras un rato en segundo plano', {
  go: async c => { c.advance(120_000); c.hide(); c.advance(300_000); c.show() },
  check: c => ({ esperado: 'se vuelve a preguntar (2 comprobaciones en total)', obtenido: c.state.updates, veredicto: ok(c.state.updates === 2) }),
})
await scenario('10. Justo en el límite de «nada más entrar» (29,9 s y 30,1 s)', {
  go: async c => { c.advance(29_900); c.swChanged(); c.advance(61_000); c.advance(0) },
  check: c => ({ esperado: 'a los 29,9 s se recarga (1)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 1) }),
})
await scenario('11. Pasados 30,1 s desde abrir la app', {
  go: async c => { c.advance(30_100); c.swChanged() },
  check: c => ({ esperado: 'se espera a que salga (0)', obtenido: c.state.reloads, veredicto: ok(c.state.reloads === 0) }),
})

Date.now = realNow
let fallos = 0
for (const r of results) {
  console.log(`${r.veredicto}  ${r.name}\n      esperado: ${r.esperado} | obtenido: ${r.obtenido}`)
  for (const l of r.logs) console.log(`      consola: ${l}`)
  if (r.veredicto !== 'OK') fallos++
}
console.log(`\n${results.length - fallos} de ${results.length} casos correctos`)
process.exit(fallos ? 1 : 0)
