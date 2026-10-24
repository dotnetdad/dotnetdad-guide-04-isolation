namespace Tickets.Reports;

public record Report(Totals Totals, List<SoldSeat> Seats);
public record Totals(int Sold, decimal Revenue, int Free);
public record SoldSeat(int SeatId, string Phone);
