# Общее для сценариев «Что попробовать»: адрес сервиса и запросы к базе.
API="${API:-http://localhost:5080}"
ANYA="+79990000001"
BORIS="+79990000002"

cd "$(dirname "$0")/.."

sql() {
  docker compose exec -T db psql -U tickets -d tickets -q -t -A -c "$1"
}

# Покупка: концерт, место, телефон, кто покупает. Печатает HTTP-код ответа.
buy() {
  local code
  code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/buy" \
    -H 'Content-Type: application/json' \
    -d "{\"concertId\":$1,\"seatId\":$2,\"phone\":\"$3\"}")
  echo "$4, место $2: $code"
}

# Сбросить места и прогреть сервис: первый запрос после старта медленнее
# (JIT, новые подключения к базе) и перемешал бы порядок покупателей.
reset_seats() {
  sql "UPDATE seats SET sold_to = NULL;"
  for i in 1 2 3; do
    curl -s -o /dev/null -X POST "$API/buy" -H 'Content-Type: application/json' \
      -d '{"concertId":0,"seatId":0,"phone":"+70000000000"}' &
  done
  wait
}

# Отчёт по концерту: итог и сколько мест в списке на вход; второй аргумент — подпись.
report() {
  local json sold revenue free listed
  json=$(curl -s "$API/concerts/$1/report")
  sold=$(echo "$json" | grep -o '"sold":[0-9]*' | cut -d: -f2)
  revenue=$(echo "$json" | grep -o '"revenue":[0-9.]*' | cut -d: -f2 | sed 's/\.0*$//')
  free=$(echo "$json" | grep -o '"free":[0-9]*' | cut -d: -f2)
  listed=$(echo "$json" | grep -o '"seatId"' | wc -l | tr -d ' ')
  echo "${2:-Отчёт}: продано $sold на $revenue, свободно $free; в списке на вход $listed"
}
