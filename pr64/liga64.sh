B=localhost:5080; J='Content-Type: application/json'
ADM=$(curl -s -X POST $B/api/auth/login -H "$J" -d '{"username":"admin","password":"Cambia-Esta-Clave-2026!"}' | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
A="Authorization: Bearer $ADM"
sql() { docker exec frontenis-pg-test psql -U frontenis -d frontenis -tA -c "$1"; }
next() { curl -s $B/api/competitions/$1 -H "$A" | grep -o '"nextCompetitionId":[^,}]*' | head -1; }
for n in P1 P2 P3 P4 P5 P6 P7 P8; do curl -s -o /dev/null -X POST $B/api/players -H "$J" -H "$A" -d "{\"name\":\"$n\",\"position\":1}"; done
echo "== Liga rotatoria 1 (8 jugadores, 2 grupos de 4)"
L1=$(curl -s -X POST $B/api/competitions -H "$J" -H "$A" -d '{"name":"Liga","type":"RotatingLeague","pairCount":8,"groupCount":2,"pairsPerGroup":4,"pointsPerWin":3,"pointsPerLoss":0,"doubleRound":false,"qualifiersPerGroup":0}' | sed -n 's/.*"id":\([0-9]*\).*/\1/p')
curl -s -o /dev/null -w "sorteo: HTTP %{http_code}\n" -X POST $B/api/competitions/$L1/draw-rotating -H "$J" -H "$A" -d '{"playerIds":[1,2,3,4,5,6,7,8]}'
sql "UPDATE \"Competitions\" SET \"Status\"=4 WHERE \"Id\"=$L1" >/dev/null
sql "SELECT \"Status\" FROM \"Competitions\" WHERE \"Id\"=$L1" | sed 's/^/estado (4 = terminada): /'
echo "== 1. Crear la siguiente liga"
R=$(curl -s -w " HTTP %{http_code}" -X POST $B/api/competitions/$L1/next-league -H "$A"); echo "$R" | cut -c1-120
L2=$(echo "$R" | sed -n 's/.*"id":\([0-9]*\).*/\1/p')
echo "liga $L1 -> $(next $L1)"
echo "== 2. Eliminar la siguiente liga (id $L2)"
curl -s -o /dev/null -w "DELETE: HTTP %{http_code}\n" -X DELETE $B/api/competitions/$L2 -H "$A"
echo "liga $L1 -> $(next $L1)   (antes del arreglo quedaba apuntando a la liga borrada)"
echo "== 3. Volver a crear la siguiente liga"
R=$(curl -s -w " HTTP %{http_code}" -X POST $B/api/competitions/$L1/next-league -H "$A"); echo "$R" | cut -c1-120
L3=$(echo "$R" | sed -n 's/.*"id":\([0-9]*\).*/\1/p')
echo "liga $L1 -> $(next $L1)"
curl -s -o /dev/null -w "GET liga %s: HTTP %{http_code}\n" $B/api/competitions/$L3
