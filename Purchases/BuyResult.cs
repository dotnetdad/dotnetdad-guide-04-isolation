namespace Tickets.Purchases;

public enum BuyResult
{
    Ok,
    NotFound,
    Taken,
    Limit,
    Busy,
}
