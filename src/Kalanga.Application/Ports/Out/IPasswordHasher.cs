namespace Kalanga.Application.Ports.Out;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Fixed bcrypt hash used when no user exists so verify timing stays comparable.
    /// </summary>
    string TimingPadHash { get; }
}
