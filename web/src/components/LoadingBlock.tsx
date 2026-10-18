import RacketLoader from './RacketLoader'

/** Aviso de «Cargando…» dentro de una página: las raquetas y la pelota, con el texto debajo. */
export default function LoadingBlock() {
  return (
    <div className="loading-block" role="status">
      <RacketLoader />
      <span className="loading-text">Cargando…</span>
    </div>
  )
}
