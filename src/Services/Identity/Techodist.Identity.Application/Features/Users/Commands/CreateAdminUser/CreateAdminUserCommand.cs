using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Domain.Security;
using Techodist.Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Application.Features.Users.Commands.CreateAdminUser;

/// <summary>Создание учётной записи администратора (регистрации покупателей нет — ADR 0004).</summary>
public sealed record CreateAdminUserCommand(
    string Email,
    string DisplayName,
    string Password,
    IReadOnlyCollection<string> Roles) : IRequest<Result<Guid>>;

internal sealed class CreateAdminUserCommandHandler(
    IIdentityService identity,
    ILogger<CreateAdminUserCommandHandler> logger)
    : IRequestHandler<CreateAdminUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryCreate(request.Email, out var email))
        {
            return Result.Failure<Guid>(
                Error.Validation("identity.user.invalid_email", $"Некорректный e-mail: '{request.Email}'."));
        }

        var unknownRoles = request.Roles?.Where(role => !AdminRoles.IsKnown(role)).ToArray() ?? [];
        if (unknownRoles.Length != 0)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "identity.user.invalid_role",
                    $"Неизвестные роли: {string.Join(", ", unknownRoles)}. Допустимые роли: {string.Join(", ", AdminRoles.All)}."));
        }

        var roles = AdminRoles.Normalize(request.Roles);
        if (roles.Count == 0)
        {
            return Result.Failure<Guid>(
                Error.Validation("identity.user.roles_required", "Нужно указать хотя бы одну роль."));
        }

        var violations = PasswordPolicy.Default.Validate(request.Password, email!.Value);
        if (violations.Count > 0)
        {
            return Result.Failure<Guid>(
                Error.Validation("identity.password.weak", string.Join(" ", violations)));
        }

        var result = await identity.CreateAsync(
            email.Value,
            request.DisplayName.Trim(),
            request.Password,
            roles,
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Создан администратор {Email} с ролями {Roles}",
                email.Value,
                string.Join(", ", roles));
        }

        return result;
    }
}
