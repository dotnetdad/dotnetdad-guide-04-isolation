using Tickets.Purchases;

namespace Tickets.Mistakes;

// Анти-примеры включаются настройкой Mistakes__Имя=true (имя — класс из Mistakes/).
// Включён один — эндпоинт получает его вместо настоящего обработчика.
public static class MistakeSwitch
{
    private static readonly Type[] BuyMistakes =
        [typeof(LostUpdate), typeof(RepeatableReadBuy)];

    public static void AddHandlers(this WebApplicationBuilder builder)
    {
        var on = builder.Configuration.GetSection("Mistakes")
            .GetChildren()
            .Where(f => bool.TryParse(f.Value, out var v) && v)
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var services = builder.Services;

        var unknown = on.Where(n => !BuyMistakes.Any(t =>
            t.Name.Equals(n, StringComparison.OrdinalIgnoreCase))).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"Нет такого анти-примера: {string.Join(", ", unknown)}. " +
                $"На этом теге есть: {string.Join(", ", BuyMistakes.Select(t => t.Name))}.");

        var buy = One(on, BuyMistakes, "покупки");
        services.AddScoped(typeof(IBuyHandler), buy ?? typeof(BuyHandler));

        if (buy is not null)
            Console.WriteLine($"Анти-пример включён: {buy.Name}");
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
