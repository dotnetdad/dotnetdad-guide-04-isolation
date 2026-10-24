namespace Tickets.Purchases;

public record BuyRequest(int ConcertId, int SeatId, string Phone);
