using FluentValidation;
using Techodist.Identity.Domain.ValueObjects;

namespace Techodist.Identity.Application.Features.Auth.Commands.AuthenticateAdmin;

public sealed class AuthenticateAdminCommandValidator : AbstractValidator<AuthenticateAdminCommand>
{
    public AuthenticateAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail обязателен.")
            .MaximumLength(EmailAddress.MaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.");
    }
}
