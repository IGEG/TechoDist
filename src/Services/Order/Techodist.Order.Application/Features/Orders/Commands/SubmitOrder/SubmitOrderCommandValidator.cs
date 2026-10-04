using FluentValidation;
using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Application.Features.Orders.Commands.SubmitOrder;

/// <summary>
/// Валидация контактов гостя. Honeypot и rate-limit публичной формы живут на шлюзе,
/// здесь проверяется то, без чего заявку нельзя обработать.
/// </summary>
public sealed class SubmitOrderCommandValidator : AbstractValidator<SubmitOrderCommand>
{
    public SubmitOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Имя обязательно.")
            .MaximumLength(200).WithMessage("Имя не должно превышать 200 символов.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("E-mail обязателен.")
            .EmailAddress().WithMessage("E-mail указан неверно.")
            .MaximumLength(256).WithMessage("E-mail не должен превышать 256 символов.");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(50).WithMessage("Телефон не должен превышать 50 символов.")
            .Matches(@"^[0-9+()\-\s]*$").WithMessage("Телефон может содержать только цифры и символы +()-.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhone));

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Комментарий не должен превышать 2000 символов.");

        RuleFor(x => x.BasketId)
            .NotEmpty().WithMessage("Корзина обязательна.");

        RuleFor(x => x.PreferredChannel)
            .IsInEnum().WithMessage("Неизвестный канал связи.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Неизвестный приоритет заявки.");
    }
}
