using Techodist.Basket.Domain.Entities;
using FluentValidation;

namespace Techodist.Basket.Application.Features.Baskets.Commands.AddBasketItem;

public sealed class AddBasketItemCommandValidator : AbstractValidator<AddBasketItemCommand>
{
    public AddBasketItemCommandValidator()
    {
        RuleFor(x => x.BasketId)
            .NotEmpty().WithMessage("Идентификатор корзины обязателен.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Товар обязателен.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, BasketItem.MaxQuantity)
            .WithMessage($"Количество должно быть от 1 до {BasketItem.MaxQuantity}.");
    }
}
