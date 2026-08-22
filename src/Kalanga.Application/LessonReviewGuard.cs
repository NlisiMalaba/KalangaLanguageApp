using Kalanga.Domain.Entities;
using Kalanga.Domain.Enums;
using Kalanga.Domain.Exceptions;

namespace Kalanga.Application;

internal static class LessonReviewGuard
{
    public static void EnsureCanReview(User actor)
    {
        EnsureActive(actor);

        if (actor.Role is not (Role.Reviewer or Role.Admin))
        {
            throw new UnauthorizedRoleException("review lessons", Role.Reviewer);
        }
    }

    public static void EnsureAdmin(User actor, string action)
    {
        EnsureActive(actor);

        if (actor.Role != Role.Admin)
        {
            throw new UnauthorizedRoleException(action, Role.Admin);
        }
    }

    private static void EnsureActive(User actor)
    {
        if (actor.IsSuspended)
        {
            throw new UserSuspendedException();
        }
    }
}
