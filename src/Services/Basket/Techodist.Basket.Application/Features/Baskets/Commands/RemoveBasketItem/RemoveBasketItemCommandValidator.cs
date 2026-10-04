using FluentValidation;

namespace Techodist.Basket.Application.Features.Baskets.Commands.RemoveBasketItem;

public sealed class RemoveBasketItemCommandValidator : AbstractValidator<RemoveBasketItemCommand>
{
    public RemoveBasketItemCommandValidator()
    {
        RuleFor(x => x.BasketId)
            .NotEmpty().WithMessage("Идентификатор корзины обязателен.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Товар обязателен.");
    }
}
