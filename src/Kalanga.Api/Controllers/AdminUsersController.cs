using Kalanga.Api.Authorization;
using Kalanga.Api.Contracts.Admin;
using Kalanga.Application.Dtos;
using Kalanga.Application.Ports.In;
using Kalanga.Domain.Enums;
using Kalanga.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kalanga.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.AdminOnly)]
[Route("admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ListUsersResult>> List(
        [FromServices] ListUsersPort listUsers,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await listUsers.ExecuteAsync(
            new ListUsersCommand(User.GetLanguageId(), User.GetUserId(), skip, take),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<ActionResult<ManageUserRoleResult>> ChangeRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        [FromServices] ManageUserRolePort manageUserRole,
        CancellationToken cancellationToken)
    {
        var result = await manageUserRole.ExecuteAsync(
            new ManageUserRoleCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                UserId.From(id),
                ManageUserAction.ChangeRole,
                request.Role),
            cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ManageUserRoleResult>> ChangeStatus(
        Guid id,
        [FromBody] ChangeUserStatusRequest request,
        [FromServices] ManageUserRolePort manageUserRole,
        CancellationToken cancellationToken)
    {
        var action = request.Status switch
        {
            UserStatus.Suspended => ManageUserAction.Suspend,
            UserStatus.Active => ManageUserAction.Reactivate,
            _ => throw new ArgumentOutOfRangeException(nameof(request.Status), request.Status, "Unsupported status."),
        };

        var result = await manageUserRole.ExecuteAsync(
            new ManageUserRoleCommand(
                User.GetLanguageId(),
                User.GetUserId(),
                UserId.From(id),
                action,
                NewRole: null),
            cancellationToken);

        return Ok(result);
    }
}
