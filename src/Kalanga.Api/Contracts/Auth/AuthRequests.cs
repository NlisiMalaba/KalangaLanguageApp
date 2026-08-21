namespace Kalanga.Api.Contracts.Auth;

public sealed record RegisterRequest(Guid LanguageId, string Email, string Password, string DisplayName);

public sealed record LoginRequest(Guid LanguageId, string Email, string Password);

public sealed record RefreshRequest(Guid LanguageId, string RefreshToken);

public sealed record LogoutRequest(Guid LanguageId, string RefreshToken);
