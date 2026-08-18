namespace Kalanga.Domain;

internal static class Guard
{
    public static Guid NotEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{name} cannot be empty.", name);
        }

        return value;
    }

    public static string Required(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be at most {maxLength} characters.", name);
        }

        return trimmed;
    }

    public static string RequiredText(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        return value.Trim();
    }

    public static int NonNegative(int value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, name);
        return value;
    }
}
