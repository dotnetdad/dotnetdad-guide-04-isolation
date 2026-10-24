using Microsoft.EntityFrameworkCore;

namespace Tickets.Data;

public static class DatabaseSetup
{
    // Концерт 7 — места 1–200, концерт 8 — места 201–400, всё по 3000.
    public static async Task PrepareDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketsDb>();
        await db.Database.MigrateAsync();
        if (await db.Seats.AnyAsync()) return;

        for (var id = 1; id <= 400; id++)
            db.Seats.Add(new Seat { Id = id, ConcertId = id <= 200 ? 7 : 8, Price = 3000 });
        await db.SaveChangesAsync();
    }
}
