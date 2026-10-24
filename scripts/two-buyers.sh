#!/usr/bin/env bash
# Аня и Борис в одну секунду покупают место 14 концерта 7.
source "$(dirname "$0")/common.sh"

reset_seats
buy 7 14 "$ANYA" "Аня" &
sleep 0.1
buy 7 14 "$BORIS" "Борис" &
wait
echo "В базе место 14 за: $(sql "SELECT coalesce(sold_to, 'никем') FROM seats WHERE id = 14;")"
