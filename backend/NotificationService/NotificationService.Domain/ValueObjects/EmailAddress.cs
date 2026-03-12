namespace NotificationService.Domain.ValueObjects;

public record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty");

        if (!IsValidEmail(value))
            throw new ArgumentException("Invalid email address format");

        Value = value.ToLower().Trim();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public static implicit operator EmailAddress(string email) => new(email);

    public override string ToString() => Value;

    public string GetDomain()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex > 0 ? Value[(atIndex + 1)..] : string.Empty;
    }

    public string GetUsername()
    {
        var atIndex = Value.IndexOf('@');
        return atIndex > 0 ? Value[..atIndex] : Value;
    }
}