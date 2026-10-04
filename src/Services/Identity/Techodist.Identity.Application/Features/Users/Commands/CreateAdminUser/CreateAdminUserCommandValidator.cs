using FluentValidation;
using Techodist.Identity.Domain.Security;
using Techodist.Identity.Domain.ValueObjects;

namespace Techodist.Identity.Application.Features.Users.Commands.CreateAdminUser;

public sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail обязателен.")
            .MaximumLength(EmailAddress.MaxLength)
            .Must(value => EmailAddress.TryCreate(value, out _)).WithMessage("Некорректный e-mail.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Отображаемое имя обязательно.")
            .MaximumLength(150);

        // Детальная политика пароля проверяется в обработчике (там известен нормализованный e-mail).
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Нужно указать хотя бы одну роль.")
            .Must(roles => roles.All(AdminRoles.IsKnown))
            .WithMessage($"Допустимые роли: {string.Join(", ", AdminRoles.All)}.");
    }
}
