using Microsoft.EntityFrameworkCore;
using Tickets.Data;
using Tickets.Reports;

namespace Tickets.Mistakes;

// Анти-пример раздела «Отчёт» (Mistakes__ReportTwoSnapshots=true): два запроса
// без транзакции, у каждого свой снимок. Покупка между ними — и итог не сходится со списком.
public class ReportTwoSnapshots(TicketsDb db) : IReportHandler
{
    public async Task<Report> GetAsync(
        int concertId, CancellationToken ct)
    {
        var all = db.Seats.Where(s => s.ConcertId == concertId);
        var totals = await all.GroupBy(s => s.ConcertId)
            .Select(g => new Totals(
                g.Count(s => s.SoldTo != null),
                g.Sum(s => s.SoldTo != null ? s.Price : 0),
                g.Count(s => s.SoldTo == null)))
            .SingleOrDefaultAsync(ct) ?? new Totals(0, 0, 0);
        var seats = await all.Where(s => s.SoldTo != null)
            .OrderBy(s => s.Id)
            .Select(s => new SoldSeat(s.Id, s.SoldTo!))
            .ToListAsync(ct);
        return new Report(totals, seats);
    }
}
