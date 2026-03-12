namespace NotificationService.Domain.ValueObjects;

public record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be empty");

        // Basic validation - in production you might want more sophisticated validation
        var cleaned = new string(value.Where(char.IsDigit).ToArray());

        if (cleaned.Length < 10)
            throw new ArgumentException("Phone number too short");

        Value = cleaned;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static implicit operator PhoneNumber(string phone) => new(phone);

    public override string ToString() => Value;

    public string FormatInternational()
    {
        if (Value.Length == 10)
        {
            return $"+1{Value}"; // Assuming US numbers for simplicity
        }
        return $"+{Value}";
    }

    public string FormatLocal()
    {
        if (Value.Length == 10)
        {
            return $"({Value[..3]}) {Value[3..6]}-{Value[6..]}";
        }
        return Value;
    }
}