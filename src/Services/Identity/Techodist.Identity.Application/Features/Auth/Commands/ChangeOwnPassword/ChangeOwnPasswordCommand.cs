using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Auth.Commands.ChangeOwnPassword;

/// <summary>Смена собственного пароля (проверяется текущий пароль).</summary>
public sealed record ChangeOwnPasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;

internal sealed class ChangeOwnPasswordCommandHandler(
    IIdentityService identity,
    ILogger<ChangeOwnPasswordCommandHandler> logger)
    : IRequestHandler<ChangeOwnPasswordCommand, Result>
{
    public async Task<Result> Handle(ChangeOwnPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await identity.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(
                Error.NotFound("identity.user.not_found", $"Администратор '{request.UserId}' не найден."));
        }

        var violations = PasswordPolicy.Default.Validate(request.NewPassword, user.Email);
        if (violations.Count > 0)
        {
            return Result.Failure(Error.Validation("identity.password.weak", string.Join(" ", violations)));
        }

        var result = await identity.ChangePasswordAsync(
            request.UserId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Администратор {Email} сменил пароль", user.Email);
        }

        return result;
    }
}
