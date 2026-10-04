using FluentValidation;

namespace Techodist.Identity.Application.Features.Users.Commands.ResetAdminUserPassword;

public sealed class ResetAdminUserPasswordCommandValidator : AbstractValidator<ResetAdminUserPasswordCommand>
{
    public ResetAdminUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Идентификатор администратора обязателен.");

        // Полные требования политики проверяются в обработчике (там известен e-mail).
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Новый пароль обязателен.");
    }
}
