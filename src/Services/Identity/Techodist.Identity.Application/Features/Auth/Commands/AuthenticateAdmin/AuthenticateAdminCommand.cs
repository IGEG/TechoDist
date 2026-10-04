using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Auth.Commands.AuthenticateAdmin;

/// <summary>
/// Аутентификация администратора по паролю. Вызывается из token-endpoint OpenIddict
/// (grant_type=password), результат используется для выпуска access/refresh-токенов.
/// </summary>
public sealed record AuthenticateAdminCommand(string Email, string Password) : IRequest<Result<AuthenticatedAdminDto>>;

internal sealed class AuthenticateAdminCommandHandler(
    IIdentityService identity,
    ILogger<AuthenticateAdminCommandHandler> logger)
    : IRequestHandler<AuthenticateAdminCommand, Result<AuthenticatedAdminDto>>
{
    public async Task<Result<AuthenticatedAdminDto>> Handle(
        AuthenticateAdminCommand request,
        CancellationToken cancellationToken)
    {
        var result = await identity.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Неудачная попытка входа администратора {Email}: {Reason}",
                request.Email,
                result.Error.Code);
        }

        return result;
    }
}
