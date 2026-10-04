using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Identity.Api.Contracts;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Features.Auth.Commands.ChangeOwnPassword;
using Techodist.Identity.Application.Features.Users.Queries.GetAdminProfile;

namespace Techodist.Identity.Api.Controllers;

/// <summary>
/// Собственный профиль администратора: доступно любой аутентифицированной роли (Admin и Manager),
/// поскольку данные ограничены claims текущего access-токена.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class AuthController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Профиль текущего администратора (для админ-панели).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetAdminProfileQuery(userId), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Смена собственного пароля: для подтверждения требуется текущий пароль.</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangeOwnPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new ChangeOwnPasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblemResult();
    }
}
