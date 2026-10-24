using System.Data;
using Microsoft.EntityFrameworkCore;
using Tickets.Data;
using Tickets.Purchases;

namespace Tickets.Mistakes;

// Анти-пример раздела «Место 14» (Mistakes__RepeatableReadBuy=true): та же покупка
// на Repeatable Read. Двойной продажи нет, но второй покупатель получает 40001 и 500.
public class RepeatableReadBuy(TicketsDb db) : IBuyHandler
{
    public async Task<BuyResult> BuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(
                IsolationLevel.RepeatableRead, ct);
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
