using FluentValidation;
using Kalanga.Application.Dtos;

namespace Kalanga.Application.Validation;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class AuthenticateUserCommandValidator : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(2048);
    }
}

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(2048);
    }
}

public sealed class ListUsersCommandValidator : AbstractValidator<ListUsersCommand>
{
    public ListUsersCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public sealed class GetPlatformMetricsCommandValidator : AbstractValidator<GetPlatformMetricsCommand>
{
    public GetPlatformMetricsCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
    }
}

public sealed class BrowseLessonCatalogCommandValidator : AbstractValidator<BrowseLessonCatalogCommand>
{
    public BrowseLessonCatalogCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
    }
}

public sealed class GetLessonCommandValidator : AbstractValidator<GetLessonCommand>
{
    public GetLessonCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
    }
}
