import type { GroupStandings } from '../api/types'

export default function StandingsTable({ group, showQualifiers, individual = false }: { group: GroupStandings; showQualifiers: boolean; individual?: boolean }) {
  return (
    <section className="card">
      <h3>{group.groupName}</h3>
      <div className="table-wrap">
        <table className="standings">
          <thead>
            <tr>
              <th>#</th><th className="left">{individual ? 'Jugador' : 'Pareja'}</th><th title="Jugados">PJ</th><th title="Ganados">G</th>
              <th title="Perdidos">P</th><th className="opt" title="Tantos a favor">TF</th><th className="opt" title="Tantos en contra">TC</th>
              <th title="Diferencia">Dif</th><th title="Puntos de clasificación">Pts</th>
            </tr>
          </thead>
          <tbody>
            {group.rows.map(r => (
              <tr key={r.entryId} className={(showQualifiers && r.qualifies) || r.movement > 0 ? 'qualifies' : r.movement < 0 ? 'relegated' : ''}>
                <td>{r.rank}</td>
                <td className="left">
                  {r.pairName}
                  {r.movement > 0 && <span className="move up" title="Sube de grupo"> ▲</span>}
                  {r.movement < 0 && <span className="move down" title="Baja de grupo"> ▼</span>}
                </td>
                <td>{r.played}</td><td>{r.won}</td><td>{r.lost}</td>
                <td className="opt">{r.pointsFor}</td><td className="opt">{r.pointsAgainst}</td>
                <td>{r.pointsDiff > 0 ? `+${r.pointsDiff}` : r.pointsDiff}</td>
                <td><strong>{r.classificationPoints}</strong></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}
