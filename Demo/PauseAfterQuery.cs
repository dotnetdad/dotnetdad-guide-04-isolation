using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Tickets.Demo;

// Пауза после каждого запроса к базе внутри HTTP-запроса: гонка двух покупок
// длится миллисекунды, а с паузой её видно руками (Demo__PauseMs, 0 — выключено).
public sealed class PauseAfterQuery(IConfiguration config, IHttpContextAccessor http)
    : DbCommandInterceptor
{
    private TimeSpan Pause => TimeSpan.FromMilliseconds(config.GetValue("Demo:PauseMs", 0));

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        await PauseAsync(cancellationToken);
        return result;
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        await PauseAsync(cancellationToken);
        return result;
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        await PauseAsync(cancellationToken);
        return result;
    }

    private Task PauseAsync(CancellationToken ct) =>
        http.HttpContext is null || Pause <= TimeSpan.Zero
            ? Task.CompletedTask
            : Task.Delay(Pause, ct);
}

public static class DemoSetup
{
    public static void AddDemoPause(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<PauseAfterQuery>();
        builder.Services.ConfigureDbContext<Tickets.Data.TicketsDb>((sp, o) =>
            o.AddInterceptors(sp.GetRequiredService<PauseAfterQuery>()));
    }
}
