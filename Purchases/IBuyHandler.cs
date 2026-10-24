namespace Tickets.Purchases;

public interface IBuyHandler
{
    Task<BuyResult> BuyAsync(BuyRequest r, CancellationToken ct);
}
