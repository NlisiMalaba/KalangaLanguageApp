using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;

namespace Kalanga.Domain.Entities;

public sealed class User
{
    internal User(
        UserId id,
        LanguageId languageId,
        string email,
        string passwordHash,
        string displayName,
        Role role,
        UserStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        LanguageId = languageId;
        Email = email;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        Role = role;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public UserId Id { get; }

    public LanguageId LanguageId { get; }

    public string Email { get; }

    public string PasswordHash { get; private set; }

    public string DisplayName { get; private set; }

    public Role Role { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsSuspended => Status == UserStatus.Suspended;

    public static User Register(
        LanguageId languageId,
        string email,
        string passwordHash,
        string displayName,
        DateTimeOffset utcNow)
    {
        var normalizedEmail = Guard.Required(email, nameof(email), 255).ToLowerInvariant();
        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new ArgumentException("Email must be a valid address.", nameof(email));
        }

        return new User(
            UserId.New(),
            languageId,
            normalizedEmail,
            Guard.Required(passwordHash, nameof(passwordHash), 255),
            Guard.Required(displayName, nameof(displayName), 100),
            Role.Learner,
            UserStatus.Active,
            utcNow,
            utcNow);
    }

    public void ChangeRole(Role role, DateTimeOffset utcNow)
    {
        Role = role;
        Touch(utcNow);
    }

    public void Suspend(DateTimeOffset utcNow)
    {
        Status = UserStatus.Suspended;
        Touch(utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        Status = UserStatus.Active;
        Touch(utcNow);
    }

    public void ReplacePasswordHash(string passwordHash, DateTimeOffset utcNow)
    {
        PasswordHash = Guard.Required(passwordHash, nameof(passwordHash), 255);
        Touch(utcNow);
    }

    private void Touch(DateTimeOffset utcNow) => UpdatedAt = utcNow;
}
