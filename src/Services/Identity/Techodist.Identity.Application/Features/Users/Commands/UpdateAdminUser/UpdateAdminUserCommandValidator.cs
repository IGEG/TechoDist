using FluentValidation;
using Techodist.Identity.Domain.Security;

namespace Techodist.Identity.Application.Features.Users.Commands.UpdateAdminUser;

public sealed class UpdateAdminUserCommandValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Идентификатор администратора обязателен.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Отображаемое имя обязательно.")
            .MaximumLength(150);

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Нужно указать хотя бы одну роль.")
            .Must(roles => roles.All(AdminRoles.IsKnown))
            .WithMessage($"Допустимые роли: {string.Join(", ", AdminRoles.All)}.");
    }
}
