namespace Tickets.Purchases;

// Телефон к одному формату (E.164): «8 999 000-00-01» и «+7 (999) 000-00-01» —
// один покупатель, иначе лимит обходится сменой записи номера.
public static class Phones
{
    public static bool TryToE164(string? phone, out string e164)
    {
        e164 = "";
        if (string.IsNullOrWhiteSpace(phone)
            || !phone.All(c => char.IsAsciiDigit(c) || " +-().".Contains(c)))
            return false;
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 10) digits = "7" + digits;
        if (digits.Length == 11 && digits[0] == '8') digits = "7" + digits[1..];
        if (digits.Length is < 11 or > 15) return false;
        e164 = "+" + digits;
        return true;
    }
}
