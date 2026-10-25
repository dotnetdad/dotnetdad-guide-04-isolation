using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tickets.Data;
using Tickets.Sms;

namespace Tickets.Purchases;

public class BuyHandler(TicketsDb db, ISmsQueue sms)
    : IBuyHandler
{
    public async Task<BuyResult> BuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            var result = await strategy.ExecuteAsync(
                token => TryBuyAsync(r, token), ct);
            if (result == BuyResult.Ok)
                sms.Enqueue(r.Phone, "Билет ваш");
            return result;
        }
        catch (RetryLimitExceededException)
        {
            return BuyResult.Busy;
        }
    }

    private const int MaxSeatsPerPhone = 4;

    private async Task<BuyResult> TryBuyAsync(
        BuyRequest r, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable, ct);
        if (await IsMineAsync(r, ct)) return BuyResult.Ok;
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

    private Task<bool> IsMineAsync(
        BuyRequest r, CancellationToken ct) =>
        db.Seats.AnyAsync(s => s.Id == r.SeatId
            && s.ConcertId == r.ConcertId
            && s.SoldTo == r.Phone, ct);

    private async Task<BuyResult> TakenOrMissingAsync(
        BuyRequest r, CancellationToken ct)
    {
        var exists = await db.Seats.AnyAsync(s =>
            s.Id == r.SeatId && s.ConcertId == r.ConcertId, ct);
        return exists ? BuyResult.Taken : BuyResult.NotFound;
    }
}
