#!/usr/bin/env bash
# Лимит — четыре билета на телефон. У Бориса уже три на концерт 7 (места 11–13),
# и он покупает в двух вкладках сразу: место 15 и место 16.
source "$(dirname "$0")/common.sh"

reset_seats
sql "UPDATE seats SET sold_to = '$BORIS' WHERE id IN (11, 12, 13);"
since=$(date -u +%Y-%m-%dT%H:%M:%SZ)
buy 7 15 "$BORIS" "Борис, вкладка 1" &
sleep 0.1
buy 7 16 "$BORIS" "Борис, вкладка 2" &
wait
echo "У Бориса билетов: $(sql "SELECT count(*) FROM seats WHERE concert_id = 7 AND sold_to = '$BORIS';")" \
  "(места $(sql "SELECT string_agg(id::text, ', ' ORDER BY id) FROM seats WHERE sold_to = '$BORIS';"))"
sleep 1
echo "СМС «Билет ваш» в логе: $(docker compose logs --since "$since" api | grep -c 'СМС на')"
echo "Повторов стратегии в логе: $(docker compose logs --since "$since" api | grep -c 'will be retried')"
