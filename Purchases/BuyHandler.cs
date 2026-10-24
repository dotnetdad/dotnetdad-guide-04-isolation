using Microsoft.EntityFrameworkCore;
using Tickets.Data;

namespace Tickets.Purchases;

public class BuyHandler(TicketsDb db) : IBuyHandler
{
    public async Task<BuyResult> BuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(ct);
        var seat = await db.Seats.SingleOrDefaultAsync(
            s => s.Id == r.SeatId
                && s.ConcertId == r.ConcertId, ct);
        if (seat is null) return BuyResult.NotFound;
        if (seat.SoldTo is not null) return BuyResult.Taken;
        seat.SoldTo = r.Phone;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return BuyResult.Ok;
    }
}
