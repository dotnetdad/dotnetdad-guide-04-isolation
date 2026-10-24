#!/usr/bin/env bash
# Концерт 7: продано 120 мест из 200. Организатор открывает отчёт,
# а между двумя его запросами Аня покупает место 14.
source "$(dirname "$0")/common.sh"

reset_seats
sql "UPDATE seats SET sold_to = '+7999' || lpad(id::text, 7, '0') WHERE id BETWEEN 81 AND 200;"
report 7 "Отчёт посреди покупки" &
sleep 0.2
buy 7 14 "$ANYA" "Аня"
wait
report 7 "Следующий отчёт"
