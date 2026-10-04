using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Identity.Api.Contracts;
using Techodist.Identity.Application.Features.Users.Commands.CreateAdminUser;
using Techodist.Identity.Application.Features.Users.Commands.ResetAdminUserPassword;
using Techodist.Identity.Application.Features.Users.Commands.SetAdminUserStatus;
using Techodist.Identity.Application.Features.Users.Commands.UpdateAdminUser;
using Techodist.Identity.Application.Features.Users.Queries.GetAdminProfile;
using Techodist.Identity.Application.Features.Users.Queries.GetAdminUsers;
using Techodist.Identity.Domain.Security;

namespace Techodist.Identity.Api.Controllers;

/// <summary>
/// Управление учётными записями администраторов. Доступно только роли <see cref="AdminRoles.Admin"/>:
/// роль <see cref="AdminRoles.Manager"/> работает с заявками в Order API, а не с администраторами.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = AdminRoles.Admin)]
public sealed class AdminUsersController(ISender sender) : ControllerBase
{
    /// <summary>Постраничный список администраторов с поиском по e-mail и отображаемому имени.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetAdminUsersQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Карточка администратора по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAdminProfileQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Создание учётной записи администратора (регистрации покупателей нет — ADR 0004).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAdminUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, null)
            : result.Error.ToProblemResult();
    }

    /// <summary>Изменение отображаемого имени и набора ролей администратора.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateAdminUserCommand(id, request.DisplayName, request.Roles),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }

    /// <summary>Включение/отключение учётной записи (мягкая блокировка вместо удаления).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetAdminUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetAdminUserStatusCommand(id, request.IsActive), cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }

    /// <summary>Сброс пароля администратора: текущий пароль не запрашивается.</summary>
    [HttpPost("{id:guid}/password")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResetAdminUserPasswordCommand(id, request.NewPassword), cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }
}
