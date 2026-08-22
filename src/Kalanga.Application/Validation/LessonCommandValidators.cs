using FluentValidation;
using Kalanga.Application.Dtos;
using Kalanga.Domain;

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

public sealed class UploadAudioCommandValidator : AbstractValidator<UploadAudioCommand>
{
    public UploadAudioCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.DurationMs).GreaterThan(0);
        RuleFor(x => x.SpeakerGender).IsInEnum();
        RuleFor(x => x.DialectLabel).MaximumLength(100).When(x => x.DialectLabel is not null);
        RuleFor(x => x)
            .Must(x => x.PhraseId is not null || x.VariationId is not null)
            .WithMessage("Audio must be associated with a phrase or a language variation.");
        RuleFor(x => x.DeclaredContentLength)
            .InclusiveBetween(1, DomainRules.MaxAudioFileSizeBytes)
            .When(x => x.DeclaredContentLength is not null);
    }
}

public sealed class ListContentPacksCommandValidator : AbstractValidator<ListContentPacksCommand>
{
    public ListContentPacksCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.Level).IsInEnum().When(x => x.Level is not null);
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category is not null);
    }
}

public sealed class GetContentPackManifestCommandValidator : AbstractValidator<GetContentPackManifestCommand>
{
    public GetContentPackManifestCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.PackId.Value).NotEmpty();
    }
}

public sealed class GetAudioCommandValidator : AbstractValidator<GetAudioCommand>
{
    public GetAudioCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.AudioRecordingId.Value).NotEmpty();
    }
}

public sealed class CompleteLessonCommandValidator : AbstractValidator<CompleteLessonCommand>
{
    public CompleteLessonCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0, DomainRules.MaxLessonScore);
    }
}

public sealed class GetProgressCommandValidator : AbstractValidator<GetProgressCommand>
{
    public GetProgressCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
    }
}

public sealed class UpdateStreakCommandValidator : AbstractValidator<UpdateStreakCommand>
{
    public UpdateStreakCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
    }
}

public sealed class AdvanceLevelCommandValidator : AbstractValidator<AdvanceLevelCommand>
{
    public AdvanceLevelCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
    }
}
