using Tickets.Purchases;
using Tickets.Reports;

namespace Tickets.Mistakes;

// Анти-примеры включаются настройкой Mistakes__Имя=true (имя — класс из Mistakes/).
// Включён один — эндпоинт получает его вместо настоящего обработчика.
public static class MistakeSwitch
{
    private static readonly Type[] BuyMistakes =
        [typeof(LostUpdate), typeof(RepeatableReadBuy)];

    private static readonly Type[] ReportMistakes =
        [typeof(ReportTwoSnapshots)];

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
        services.AddScoped(typeof(IBuyHandler), buy ?? typeof(BuyHandler));
        services.AddScoped(typeof(IReportHandler), report ?? typeof(ReportHandler));

        foreach (var t in new[] { buy, report }.OfType<Type>())
            Console.WriteLine($"Анти-пример включён: {t.Name}");
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
