import { useEffect, useRef, useState, type ReactNode } from 'react'
import SubTabs from './SubTabs'
import { isStandalone } from '../device'

type Platform = 'android' | 'iphone'

const DISMISS_KEY = 'frontenis.tips.install.dismissed'
const STEP_MS = 2600

const readDismissed = () => { try { return localStorage.getItem(DISMISS_KEY) === '1' } catch { return false } }
const writeDismissed = () => { try { localStorage.setItem(DISMISS_KEY, '1') } catch { /* sin almacenamiento */ } }


const isIphone = () => /iphone|ipad|ipod/i.test(navigator.userAgent)
const prefersReducedMotion = () => window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false

interface Tutorial { name: string; browser: string; steps: { title: string; text: string }[] }

const TUTORIALS: Record<Platform, Tutorial> = {
  android: {
    name: 'Android', browser: 'Chrome',
    steps: [
      { title: 'Abre el enlace en Chrome', text: 'Toca los tres puntos de arriba a la derecha.' },
      { title: 'Desliza el menú y elige «Instalar y crear acceso directo»', text: 'Está al final de la lista. Según el móvil se llama «Instalar aplicación» o «Añadir a la pantalla de inicio».' },
      { title: 'Pulsa «Instalar»', text: 'Confirma en la ventana que aparece.' },
      { title: '¡Listo!', text: 'La app queda en tu pantalla de inicio, como cualquier otra.' },
    ],
  },
  iphone: {
    name: 'iPhone', browser: 'Safari',
    steps: [
      { title: 'Abre el enlace en Safari', text: 'Toca el botón Compartir (el cuadrado con la flecha) de la barra de abajo.' },
      { title: 'Elige «Añadir a pantalla de inicio»', text: 'Desliza hacia arriba la lista si no la ves.' },
      { title: 'Pulsa «Añadir»', text: 'Arriba a la derecha de la ventana que aparece.' },
      { title: '¡Listo!', text: 'La app queda en tu pantalla de inicio, como cualquier otra.' },
    ],
  },
}

const TEAL = '#0f766e'

/** Marco del móvil común a las dos animaciones. */
function Phone({ children, label }: { children: ReactNode; label: string }) {
  return (
    <svg className="tips-phone" viewBox="0 0 200 380" role="img" aria-label={label}>
      <rect x="1" y="1" width="198" height="378" rx="30" fill="#14211f" />
      <rect x="8" y="8" width="184" height="364" rx="23" fill="#ffffff" />
      <rect x="78" y="13" width="44" height="9" rx="4.5" fill="#14211f" />
      {children}
    </svg>
  )
}

/** Página de la app (lo que se ve dentro del navegador). */
function AppPage({ top }: { top: number }) {
  return (
    <g>
      <rect x="8" y={top} width="184" height="34" fill={TEAL} />
      <image href="/icon-192.png" x="14" y={top + 5} width="24" height="24" />
      <text x="44" y={top + 21} fontSize="10" fontWeight="700" fill="#fff">Club Frontenis</text>
      <rect x="18" y={top + 48} width="164" height="46" rx="8" fill="#eef3f2" />
      <rect x="18" y={top + 102} width="164" height="46" rx="8" fill="#eef3f2" />
      <rect x="18" y={top + 156} width="164" height="46" rx="8" fill="#eef3f2" />
      <rect x="28" y={top + 62} width="70" height="6" rx="3" fill="#c9d6d3" />
      <rect x="28" y={top + 116} width="90" height="6" rx="3" fill="#c9d6d3" />
      <rect x="28" y={top + 170} width="60" height="6" rx="3" fill="#c9d6d3" />
    </g>
  )
}

/** Pantalla de inicio del móvil con la app recién añadida. */
function HomeScreen() {
  const icons = [0, 1, 2, 3, 4, 5, 6, 7]
  const colors = ['#6c8ebf', '#d6a24c', '#7aa874', '#b57a9e', '#6aa5b0', '#c27d62', '#8a86b8', '#9aa86a']
  return (
    <g>
      <rect x="8" y="8" width="184" height="364" rx="23" fill="#2c3e50" />
      <rect x="8" y="8" width="184" height="364" rx="23" fill="url(#wall)" />
      {icons.map(i => (
        <rect key={i} x={22 + (i % 4) * 41} y={70 + Math.floor(i / 4) * 62} width="32" height="32" rx="8" fill={colors[i]} opacity=".85" />
      ))}
      <g className="tips-pop">
        <image href="/icon-192.png" x="22" y="194" width="32" height="32" />
        <text x="38" y="240" fontSize="8" fill="#fff" textAnchor="middle">Frontenis</text>
      </g>
      <rect x="64" y="362" width="72" height="4" rx="2" fill="#ffffffaa" />
    </g>
  )
}

/** Un círculo que late alrededor de lo que hay que tocar. */
const Pulse = ({ cx, cy, r }: { cx: number; cy: number; r: number }) => (
  <circle className="tips-pulse" cx={cx} cy={cy} r={r} fill="none" stroke={TEAL} strokeWidth="2.5" />
)

const AndroidBar = () => (
<g>
  <rect x="8" y="8" width="184" height="46" fill="#f1f3f4" />
  <rect x="16" y="26" width="132" height="22" rx="11" fill="#fff" stroke="#dadce0" />
  <text x="28" y="41" fontSize="9" fill="#5f6368">frontenis.app</text>
  {[0, 1, 2].map(i => <circle key={i} cx="174" cy={30 + i * 6} r="1.7" fill="#3c4043" />)}
</g>
)

const IphoneToolbar = () => (
  <g>
    <rect x="8" y="318" width="184" height="54" fill="#f6f6f6" />
    <rect x="14" y="296" width="172" height="26" rx="13" fill="#e9e9ea" />
    <text x="100" y="313" fontSize="9" fill="#3c3c43" textAnchor="middle">frontenis.app</text>
    <path d="M26 346l-6 6 6 6" stroke="#007aff" strokeWidth="2" fill="none" strokeLinecap="round" />
    <path d="M58 346l6 6-6 6" stroke="#007aff" strokeWidth="2" fill="none" strokeLinecap="round" />
    <g stroke="#007aff" strokeWidth="1.8" fill="none" strokeLinecap="round" strokeLinejoin="round">
      <path d="M100 352v-12M95 345l5-5 5 5M94 349v8h12v-8" />
    </g>
    <rect x="132" y="344" width="14" height="14" rx="2" stroke="#007aff" strokeWidth="1.8" fill="none" />
    <rect x="166" y="344" width="14" height="14" rx="2" stroke="#007aff" strokeWidth="1.8" fill="none" />
  </g>
)

/** Menú de Chrome en Android tal como se ve (fila de botones y lista larga); se desliza hasta la opción de instalar. */
const MENU_ROWS: (string | null)[] = [
  'Nueva pestaña', 'Nueva pestaña Incógnito', 'Añadir pestaña a nuevo grupo', null,
  'Historial', 'Borrar datos de navegación', 'Controles del sitio', null,
  'Descargas', 'Marcadores', 'Pestañas recientes', null,
  'Compartir…', 'Buscar en la página', 'Traducir…', 'Escuchar esta página', 'Mostrar modo Lectura',
  'Instalar y crear acceso directo',
]
const MENU_TOP = 74, ROW_H = 24, SEP_H = 10, PANEL_BOTTOM = 350
const menuLayout = (() => {
  let y = MENU_TOP
  return MENU_ROWS.map(label => { const at = y; y += label === null ? SEP_H : ROW_H; return { label, y: at } })
})()
const lastRow = menuLayout[menuLayout.length - 1]
const MENU_SCROLL = Math.max(0, lastRow.y + ROW_H - (PANEL_BOTTOM - 8))

function AndroidMenu() {
  return (
    <g>
      <defs><clipPath id="tips-menu-clip"><rect x="22" y="14" width="168" height="336" rx="14" /></clipPath></defs>
      <rect x="22" y="14" width="168" height="336" rx="14" fill="#fff" stroke="#dadce0" />
      <g clipPath="url(#tips-menu-clip)">
        <g className="tips-menu-scroll" style={{ ['--scroll' as string]: `-${MENU_SCROLL}px` }}>
          {['←', '→', '☆', '⇩', '↻'].map((g, i) => (
            <g key={g}>
              <circle cx={44 + i * 33} cy={44} r="11" fill="#f1f3f4" />
              <text x={44 + i * 33} y={48} fontSize="11" textAnchor="middle" fill="#3c4043">{g}</text>
            </g>
          ))}
          {menuLayout.map((r, i) => r.label === null
            ? <line key={i} x1="32" y1={r.y + 5} x2="180" y2={r.y + 5} stroke="#e3e3e3" />
            : r.label.startsWith('Instalar')
              ? (
                <g key={r.label}>
                  <rect x="28" y={r.y} width="156" height={ROW_H - 2} rx="6" fill={TEAL} />
                  <text x="38" y={r.y + 14} fontSize="8" fontWeight="700" fill="#fff">{r.label}</text>
                </g>
              )
              : (
                <g key={r.label}>
                  <rect x="36" y={r.y + 7} width="8" height="8" rx="2" fill="none" stroke="#9aa0a6" />
                  <text x="52" y={r.y + 14} fontSize="8" fill="#3c4043">{r.label}</text>
                </g>
              ))}
        </g>
      </g>
    </g>
  )
}

function AndroidScenes({ step }: { step: number }) {
  const scene = (i: number, children: ReactNode) => <g className={`tips-scene${step === i ? ' on' : ''}`} aria-hidden="true">{children}</g>
  return (
    <>
      {scene(0, <><AppPage top={54} /><AndroidBar /><Pulse cx={174} cy={36} r={14} /></>)}
      {scene(1, (
        <>
          <AppPage top={54} /><AndroidBar />
          <rect x="8" y="8" width="184" height="364" fill="#000" opacity=".28" />
          <AndroidMenu />
        </>
      ))}
      {scene(2, (
        <>
          <AppPage top={54} /><AndroidBar />
          <rect x="8" y="8" width="184" height="364" fill="#000" opacity=".4" />
          <rect x="22" y="130" width="156" height="110" rx="14" fill="#fff" />
          <image href="/icon-192.png" x="32" y="142" width="28" height="28" />
          <text x="68" y="154" fontSize="10" fontWeight="700" fill="#202124">¿Instalar app?</text>
          <text x="68" y="167" fontSize="8.5" fill="#5f6368">Frontenis</text>
          <text x="38" y="196" fontSize="8.5" fill="#5f6368">Se añadirá a tu pantalla de inicio.</text>
          <text x="112" y="224" fontSize="10" fontWeight="700" fill={TEAL} textAnchor="end">Cancelar</text>
          <rect x="120" y="210" width="50" height="20" rx="10" fill={TEAL} />
          <text x="145" y="224" fontSize="9" fontWeight="700" fill="#fff" textAnchor="middle">Instalar</text>
          <Pulse cx={145} cy={220} r={22} />
        </>
      ))}
      {scene(3, <HomeScreen />)}
    </>
  )
}

function IphoneScenes({ step }: { step: number }) {
  const scene = (i: number, children: ReactNode) => <g className={`tips-scene${step === i ? ' on' : ''}`} aria-hidden="true">{children}</g>
  return (
    <>
      {scene(0, <><AppPage top={30} /><IphoneToolbar /><Pulse cx={100} cy={350} r={17} /></>)}
      {scene(1, (
        <>
          <AppPage top={30} /><IphoneToolbar />
          <rect x="8" y="8" width="184" height="364" fill="#000" opacity=".35" />
          <rect x="8" y="150" width="184" height="222" rx="16" fill="#f2f2f7" />
          <rect x="16" y="160" width="168" height="40" rx="10" fill="#fff" />
          <image href="/icon-192.png" x="24" y="166" width="28" height="28" />
          <text x="60" y="184" fontSize="9" fontWeight="700" fill="#1c1c1e">Club Frontenis</text>
          <rect x="16" y="210" width="168" height="26" rx="8" fill="#fff" />
          <text x="26" y="227" fontSize="9" fill="#1c1c1e">Copiar</text>
          <rect x="16" y="240" width="168" height="26" rx="8" fill="#fff" />
          <text x="26" y="257" fontSize="9" fill="#1c1c1e">Añadir a marcadores</text>
          <rect x="16" y="270" width="168" height="26" rx="8" fill={TEAL} />
          <text x="26" y="287" fontSize="9" fontWeight="700" fill="#fff">Añadir a pantalla de inicio</text>
          <rect x="16" y="300" width="168" height="26" rx="8" fill="#fff" />
          <text x="26" y="317" fontSize="9" fill="#1c1c1e">Imprimir</text>
        </>
      ))}
      {scene(2, (
        <>
          <AppPage top={30} />
          <rect x="8" y="8" width="184" height="364" fill="#000" opacity=".35" />
          <rect x="8" y="64" width="184" height="308" rx="16" fill="#f2f2f7" />
          <text x="22" y="92" fontSize="9" fill={TEAL} fontWeight="700">Cancelar</text>
          <text x="100" y="92" fontSize="8.5" fontWeight="700" fill="#1c1c1e" textAnchor="middle">Añadir a pantalla</text>
          <text x="172" y="92" fontSize="9" fontWeight="700" fill={TEAL} textAnchor="end">Añadir</text>
          <Pulse cx={160} cy={89} r={16} />
          <rect x="16" y="106" width="168" height="48" rx="10" fill="#fff" />
          <image href="/icon-192.png" x="24" y="116" width="28" height="28" />
          <text x="62" y="134" fontSize="10" fill="#1c1c1e">Frontenis</text>
          <text x="22" y="180" fontSize="8.5" fill="#6c6c70">Se añadirá un icono a tu pantalla de inicio.</text>
        </>
      ))}
      {scene(3, <HomeScreen />)}
    </>
  )
}

/**
 * Tutorial animado (sin archivos de vídeo) de cómo instalar la app: una pestaña para Android (Chrome) y otra para iPhone
 * (Safari). Recorre los cuatro pasos en bucle, con pausa y saltos a cualquier paso, y los pasos escritos debajo.
 */
export default function InstallTips() {
  const [hidden, setHidden] = useState(() => readDismissed() || isStandalone())
  const [platform, setPlatform] = useState<Platform>(() => (isIphone() ? 'iphone' : 'android'))
  const [step, setStep] = useState(0)
  const [playing, setPlaying] = useState(() => !prefersReducedMotion())
  const [visible, setVisible] = useState(false)
  const box = useRef<HTMLElement>(null)

  // Solo avanza mientras la sección se ve en pantalla (ahorra batería).
  useEffect(() => {
    const el = box.current
    if (!el || typeof IntersectionObserver === 'undefined') { setVisible(true); return }
    const io = new IntersectionObserver(([e]) => setVisible(e.isIntersecting), { threshold: 0.3 })
    io.observe(el)
    return () => io.disconnect()
  }, [hidden])

  useEffect(() => {
    if (!playing || !visible) return
    const t = window.setInterval(() => setStep(s => (s + 1) % 4), STEP_MS)
    return () => window.clearInterval(t)
  }, [playing, visible, platform])

  if (hidden) return null
  const tut = TUTORIALS[platform]
  const label = `Cómo instalar la app en ${tut.name} con ${tut.browser}: paso ${step + 1} de 4, ${tut.steps[step].title}`

  return (
    <section ref={box} aria-labelledby="h-consejos" className="tips">
      <h2 id="h-consejos" className="section-title">Consejos</h2>
      <div className="card tips-card">
        <h3 className="tips-title">Ten la app en tu móvil</h3>
        <p className="muted">Si entras desde un enlace, instálala y la tendrás como una app más, sin buscar el enlace cada vez.</p>
        <SubTabs label="Móvil" value={platform} onChange={k => { setPlatform(k as Platform); setStep(0) }}
          options={[{ key: 'android', label: 'Android · Chrome' }, { key: 'iphone', label: 'iPhone · Safari' }]} />
        <div className="tips-body">
          <div className="tips-stage">
            <Phone label={label}>
              <defs>
                <linearGradient id="wall" x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0" stopColor="#355c7d" /><stop offset="1" stopColor="#6c5b7b" />
                </linearGradient>
              </defs>
              {platform === 'android' ? <AndroidScenes step={step} /> : <IphoneScenes step={step} />}
            </Phone>
            <button type="button" className="secondary small tips-play" aria-pressed={!playing}
              onClick={() => setPlaying(p => !p)}>{playing ? 'Pausa' : 'Reproducir'}</button>
          </div>
          <ol className="tips-steps">
            {tut.steps.map((s, i) => (
              <li key={s.title} className={i === step ? 'on' : ''}>
                <button type="button" aria-current={i === step ? 'step' : undefined} onClick={() => { setStep(i); setPlaying(false) }}>
                  <span className="tips-num" aria-hidden="true">{i + 1}</span>
                  <span><strong>{s.title}</strong><small>{s.text}</small></span>
                </button>
              </li>
            ))}
          </ol>
        </div>
        <button type="button" className="secondary small tips-hide" onClick={() => { writeDismissed(); setHidden(true) }}>Ya la tengo instalada</button>
      </div>
    </section>
  )
}
