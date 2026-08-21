using FluentValidation;
using Kalanga.Application.Dtos;

namespace Kalanga.Application.Validation;

public sealed class ManageUserRoleCommandValidator : AbstractValidator<ManageUserRoleCommand>
{
    public ManageUserRoleCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.TargetUserId.Value).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.NewRole)
            .NotNull()
            .When(x => x.Action == ManageUserAction.ChangeRole);
        RuleFor(x => x.NewRole)
            .IsInEnum()
            .When(x => x.NewRole is not null);
    }
}
