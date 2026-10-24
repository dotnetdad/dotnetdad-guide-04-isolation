namespace Tickets.Reports;

public interface IReportHandler
{
    Task<Report> GetAsync(int concertId, CancellationToken ct);
}
