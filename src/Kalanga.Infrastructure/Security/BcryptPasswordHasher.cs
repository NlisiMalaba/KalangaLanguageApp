using Kalanga.Application.Ports.Out;

namespace Kalanga.Infrastructure.Security;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public BcryptPasswordHasher()
    {
        TimingPadHash = BCrypt.Net.BCrypt.HashPassword("timing-pad-unused", WorkFactor);
    }

    public string TimingPadHash { get; }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
