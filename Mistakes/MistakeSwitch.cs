using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tickets.Data;
using Tickets.Purchases;
using Tickets.Reports;

namespace Tickets.Mistakes;

// Анти-примеры включаются настройкой Mistakes__Имя=true (имя — класс из Mistakes/).
// Включён один — эндпоинт получает его вместо настоящего обработчика.
public static class MistakeSwitch
{
    private static readonly Type[] BuyMistakes =
    [
        typeof(LostUpdate), typeof(RepeatableReadBuy),
        typeof(LimitReadCommitted), typeof(SerializableNoRetry),
    ];

    private static readonly Type[] ReportMistakes =
        [typeof(ReportTwoSnapshots), typeof(ReportOutsideStrategy)];

    // Эти анти-примеры открывают транзакцию мимо стратегии выполнения: с повтором
    // на контексте они упали бы с InvalidOperationException, а не показали свой
    // симптом. При них контекст — без повтора. Остальные повтору не мешают:
    // ReportTwoSnapshots транзакции не открывает, а у ReportOutsideStrategy
    // это исключение и есть симптом.
    private static readonly Type[] WithoutRetry =
    [
        typeof(LostUpdate), typeof(RepeatableReadBuy),
        typeof(LimitReadCommitted), typeof(SerializableNoRetry),
    ];

    public static void AddHandlers(this WebApplicationBuilder builder)
    {
        var on = builder.Configuration.GetSection("Mistakes")
            .GetChildren()
            .Where(f => bool.TryParse(f.Value, out var v) && v)
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var services = builder.Services;

        var known = BuyMistakes.Concat(ReportMistakes).ToList();
        var unknown = on.Where(n => !known.Any(t =>
            t.Name.Equals(n, StringComparison.OrdinalIgnoreCase))).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Нет такого анти-примера: {string.Join(", ", unknown)}. " +
                $"На этом теге есть: {string.Join(", ", known.Select(t => t.Name))}.");

        var buy = One(on, BuyMistakes, "покупки");
        var report = One(on, ReportMistakes, "отчёта");
        var withoutRetry = buy is not null && WithoutRetry.Contains(buy);
        if (withoutRetry && report == typeof(ReportOutsideStrategy))
            throw new InvalidOperationException(
                $"{nameof(ReportOutsideStrategy)} падает только с повтором на контексте, " +
                $"а {buy!.Name} повтор выключает. Включай их по одному.");

        services.AddScoped(typeof(IBuyHandler), buy ?? typeof(BuyHandler));
        services.AddScoped(typeof(IReportHandler), report ?? typeof(ReportHandler));
        if (withoutRetry)
            services.ConfigureDbContext<TicketsDb>(o => o.UseNpgsql(npgsql =>
                npgsql.ExecutionStrategy(d => new NonRetryingExecutionStrategy(d))));

        foreach (var t in new[] { buy, report }.OfType<Type>())
            Console.WriteLine($"Анти-пример включён: {t.Name}");
        if (withoutRetry)
            Console.WriteLine("Повтор при сбоях выключен: анти-пример открывает транзакцию мимо стратегии");
    }

    // Два анти-примера одного обработчика сразу не работают: эндпоинт берёт один.
    private static Type? One(HashSet<string> on, Type[] mistakes, string what)
    {
        var picked = mistakes.Where(t => on.Contains(t.Name)).ToList();
        if (picked.Count > 1)
            throw new InvalidOperationException(
                $"Включено несколько анти-примеров {what}: " +
                $"{string.Join(", ", picked.Select(t => t.Name))}. Включай по одному.");
        return picked.SingleOrDefault();
    }
}
