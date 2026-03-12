namespace NotificationService.Domain.ValueObjects;

public record MessageContent
{
    public string Value { get; }

    public MessageContent(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Message content cannot be empty");

        if (value.Length > 1600)
            throw new ArgumentException("Message content too long");

        Value = value.Trim();
    }

    public static implicit operator string(MessageContent content) => content.Value;
    public static implicit operator MessageContent(string content) => new(content);

    public override string ToString() => Value;

    public int WordCount => Value.Split(new[] { ' ', '\t', '\n', '\r' },
        StringSplitOptions.RemoveEmptyEntries).Length;

    public bool ContainsHtml => Value.Contains('<') && Value.Contains('>');
}