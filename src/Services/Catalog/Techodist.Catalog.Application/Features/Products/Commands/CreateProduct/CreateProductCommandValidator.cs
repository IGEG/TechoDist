using FluentValidation;

namespace Techodist.Catalog.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название товара обязательно.")
            .MaximumLength(200);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Категория обязательна.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Цена не может быть отрицательной.");

        RuleFor(x => x.VolumeLiters)
            .GreaterThan(0).When(x => x.VolumeLiters.HasValue)
            .WithMessage("Объём должен быть положительным числом.");

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500);

        RuleFor(x => x.Slug)
            .MaximumLength(200);
    }
}
