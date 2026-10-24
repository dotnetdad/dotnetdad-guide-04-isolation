namespace Tickets.Sms;

public interface ISmsQueue
{
    void Enqueue(string phone, string text);
}
