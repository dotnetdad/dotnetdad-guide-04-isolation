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
