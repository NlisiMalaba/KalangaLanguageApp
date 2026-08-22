using FluentValidation;
using Kalanga.Application.Dtos;

namespace Kalanga.Application.Validation;

public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Level).IsInEnum();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScenarioContext).MaximumLength(4000).When(x => x.ScenarioContext is not null);
        RuleFor(x => x.XpReward).GreaterThan(0).When(x => x.XpReward is not null);
    }
}

public sealed class SubmitLessonForReviewCommandValidator : AbstractValidator<SubmitLessonForReviewCommand>
{
    public SubmitLessonForReviewCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
    }
}

public sealed class SaveLessonDraftCommandValidator : AbstractValidator<SaveLessonDraftCommand>
{
    public SaveLessonDraftCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Level).IsInEnum();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScenarioContext).MaximumLength(4000).When(x => x.ScenarioContext is not null);
        RuleFor(x => x.XpReward).GreaterThan(0);
    }
}

public sealed class ListReviewQueueCommandValidator : AbstractValidator<ListReviewQueueCommand>
{
    public ListReviewQueueCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public sealed class ReviewLessonCommandValidator : AbstractValidator<ReviewLessonCommand>
{
    public ReviewLessonCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Feedback)
            .NotEmpty()
            .When(x => x.Action is ReviewLessonAction.Reject or ReviewLessonAction.RequestRevision);
    }
}

public sealed class OverrideLessonPublicationCommandValidator : AbstractValidator<OverrideLessonPublicationCommand>
{
    public OverrideLessonPublicationCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
    }
}
