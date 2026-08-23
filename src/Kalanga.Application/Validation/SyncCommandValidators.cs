using FluentValidation;
using Kalanga.Application.Dtos;
using Kalanga.Domain;

namespace Kalanga.Application.Validation;

public sealed class SyncProgressCommandValidator : AbstractValidator<SyncProgressCommand>
{
    public SyncProgressCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.ClientOperationId).NotEmpty().MaximumLength(DomainRules.MaxSyncClientOperationIdLength);
        RuleFor(x => x.Progress).NotNull().Must(items => items.Count <= DomainRules.MaxSyncProgressItems);
        RuleFor(x => x.SpacedRepetition).NotNull().Must(items => items.Count <= DomainRules.MaxSyncSrsItems);
    }
}

public sealed class PullSyncCommandValidator : AbstractValidator<PullSyncCommand>
{
    public PullSyncCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.SinceVersion).GreaterThanOrEqualTo(0);
    }
}
