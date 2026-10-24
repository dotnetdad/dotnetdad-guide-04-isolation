using System.Data;
using Microsoft.EntityFrameworkCore;
using Tickets.Data;
using Tickets.Purchases;

namespace Tickets.Mistakes;

// Анти-пример раздела «Лимит» (Mistakes__SerializableNoRetry=true): Serializable без
// повтора. Лимит держится, но вторая вкладка получает 40001 и отвечает 500.
public class SerializableNoRetry(TicketsDb db) : IBuyHandler
{
    private const int MaxSeatsPerPhone = 4;

    public async Task<BuyResult> BuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
        var mine = await db.Seats.CountAsync(s =>
            s.ConcertId == r.ConcertId
            && s.SoldTo == r.Phone, ct);
        if (mine >= MaxSeatsPerPhone) return BuyResult.Limit;
        var sold = await db.Seats
            .Where(s => s.Id == r.SeatId && s.SoldTo == null
                && s.ConcertId == r.ConcertId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.SoldTo, r.Phone), ct);
        if (sold == 0) return await TakenOrMissingAsync(r, ct);
        await tx.CommitAsync(ct);
        return BuyResult.Ok;
    }

    private async Task<BuyResult> TakenOrMissingAsync(
        BuyRequest r, CancellationToken ct)
    {
        var exists = await db.Seats.AnyAsync(s =>
            s.Id == r.SeatId && s.ConcertId == r.ConcertId, ct);
        return exists ? BuyResult.Taken : BuyResult.NotFound;
    }
}
