using Microsoft.EntityFrameworkCore;
using Tickets.Data;

namespace Tickets.Purchases;

public class BuyHandler(TicketsDb db) : IBuyHandler
{
    public async Task<BuyResult> BuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        var sold = await db.Seats
            .Where(s => s.Id == r.SeatId && s.SoldTo == null
                && s.ConcertId == r.ConcertId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.SoldTo, r.Phone), ct);
        if (sold == 1) return BuyResult.Ok;
        var exists = await db.Seats.AnyAsync(s =>
            s.Id == r.SeatId && s.ConcertId == r.ConcertId, ct);
        return exists ? BuyResult.Taken : BuyResult.NotFound;
    }
}
