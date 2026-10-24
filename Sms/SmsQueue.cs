using System.Threading.Channels;

namespace Tickets.Sms;

public record SmsMessage(string Phone, string Text);

// Очередь в памяти: покупка не ждёт провайдера, СМС отправляет SmsSender.
// При падении сервиса неотправленные СМС теряются — надёжно это делает outbox (README).
public class SmsQueue : ISmsQueue
{
    private readonly Channel<SmsMessage> channel =
        Channel.CreateUnbounded<SmsMessage>();

    public void Enqueue(string phone, string text) =>
        channel.Writer.TryWrite(new SmsMessage(phone, text));

    public IAsyncEnumerable<SmsMessage> ReadAllAsync(CancellationToken ct) =>
        channel.Reader.ReadAllAsync(ct);
}
