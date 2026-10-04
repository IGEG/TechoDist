using FluentValidation;

namespace Techodist.Order.Application.Features.Orders.Commands.ChangeOrderStatus;

public sealed class ChangeOrderStatusCommandValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    public ChangeOrderStatusCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Идентификатор заявки обязателен.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Неизвестный статус заявки.");

        RuleFor(x => x.ManagerComment)
            .MaximumLength(2000).WithMessage("Комментарий менеджера не должен превышать 2000 символов.");
    }
}
