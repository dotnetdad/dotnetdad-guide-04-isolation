namespace Tickets.Data;

public class Seat
{
    public int Id { get; set; }
    public int ConcertId { get; set; }
    public decimal Price { get; set; }
    public string? SoldTo { get; set; }
}
