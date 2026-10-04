using FluentValidation;

namespace Techodist.Identity.Application.Features.Auth.Commands.ChangeOwnPassword;

public sealed class ChangeOwnPasswordCommandValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Идентификатор администратора обязателен.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Текущий пароль обязателен.");

        // Полные требования политики проверяются в обработчике (там известен e-mail).
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Новый пароль обязателен.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Новый пароль должен отличаться от текущего.");
    }
}
