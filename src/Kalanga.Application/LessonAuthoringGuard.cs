using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;

namespace Kalanga.Application;

internal static class LessonAuthoringGuard
{
    public static void EnsureCanAuthor(User actor, string action)
    {
        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }

        if (actor.Role is not (Role.Contributor or Role.Admin))
        {
            throw new UnauthorizedRoleException(action, Role.Contributor);
        }
    }

    public static void EnsureCanMutate(User actor, Lesson lesson, string action)
    {
        EnsureCanAuthor(actor, action);

        if (actor.Role != Role.Admin && lesson.ContributorId != actor.Id)
        {
            throw new LessonAccessDeniedException(action);
        }
    }
}
