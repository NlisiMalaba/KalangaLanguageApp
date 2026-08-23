using FluentValidation;
using Kalanga.Application.Dtos;

namespace Kalanga.Application.Validation;

public sealed class SubmitRequestCommandValidator : AbstractValidator<SubmitRequestCommand>
{
    public SubmitRequestCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Description).NotEmpty();
    }
}

public sealed class ListRequestsCommandValidator : AbstractValidator<ListRequestsCommand>
{
    public ListRequestsCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).InclusiveBetween(1, 100);
    }
}

public sealed class UpvoteRequestCommandValidator : AbstractValidator<UpvoteRequestCommand>
{
    public UpvoteRequestCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.RequestId.Value).NotEmpty();
    }
}

public sealed class FulfillRequestCommandValidator : AbstractValidator<FulfillRequestCommand>
{
    public FulfillRequestCommandValidator()
    {
        RuleFor(x => x.LanguageId.Value).NotEmpty();
        RuleFor(x => x.ActorUserId.Value).NotEmpty();
        RuleFor(x => x.RequestId.Value).NotEmpty();
        RuleFor(x => x.LessonId.Value).NotEmpty();
    }
}
