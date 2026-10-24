using Microsoft.EntityFrameworkCore;

namespace Tickets.Data;

public class TicketsDb(DbContextOptions<TicketsDb> options)
    : DbContext(options)
{
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Seat>()
            .Property(s => s.Price)
            .HasPrecision(10, 2);
    }
}
