#!/usr/bin/env bash
# Старт продаж: восемь разных покупателей в одну секунду берут восемь разных мест.
# На Serializable часть из них получает 40001 — ложные конфликты, и стратегия
# повторяет; у кого кончились повторы — 503 с заголовком Retry-After.
source "$(dirname "$0")/common.sh"

reset_seats
since=$(date -u +%Y-%m-%dT%H:%M:%SZ)
for i in 1 2 3 4 5 6 7 8; do
  buy 7 $((30 + i)) "+7999100000$i" "Покупатель $i" &
done
wait
sleep 1
echo "Повторов стратегии в логе: $(docker compose logs --since "$since" api | grep -c 'will be retried')"
echo "План подсчёта лимита:"
sql "EXPLAIN SELECT count(*) FROM seats WHERE concert_id = 7 AND sold_to = '+79990000002';"
