import { useDocumentTitle } from '../hooks'
import StopwatchPanel from '../components/StopwatchPanel'

export default function TimerPage() {
  useDocumentTitle('Cronómetro')
  return (
    <>
      <h1>Cronómetro</h1>
      <StopwatchPanel />
    </>
  )
}
