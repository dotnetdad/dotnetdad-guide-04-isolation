namespace Tickets.Sms;

public interface ISmsProvider
{
    Task SendAsync(SmsMessage sms, CancellationToken ct);
}

// Учебный провайдер: вместо отправки пишет СМС в лог.
public class LogSmsProvider(ILogger<LogSmsProvider> log) : ISmsProvider
{
    public Task SendAsync(SmsMessage sms, CancellationToken ct)
    {
        log.LogInformation("СМС на {Phone}: {Text}", sms.Phone, sms.Text);
        return Task.CompletedTask;
    }
}

public class SmsSender(SmsQueue queue, ISmsProvider provider, ILogger<SmsSender> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var sms in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await provider.SendAsync(sms, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "СМС на {Phone} не ушла", sms.Phone);
            }
        }
    }
}

public static class SmsSetup
{
    public static void AddSms(this IServiceCollection services)
    {
        services.AddSingleton<SmsQueue>();
        services.AddSingleton<ISmsQueue>(sp => sp.GetRequiredService<SmsQueue>());
        services.AddSingleton<ISmsProvider, LogSmsProvider>();
        services.AddHostedService<SmsSender>();
    }
}
