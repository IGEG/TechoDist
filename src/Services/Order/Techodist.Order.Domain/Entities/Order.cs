using Techodist.BuildingBlocks.Core.Entities;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Domain.Enums;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Domain.Entities;

/// <summary>
/// Заявка (агрегат). Онлайн-оплаты нет: заявка — это обращение клиента, которое менеджер
/// ведёт по статусам, а письма клиенту и магазину отправляет Notification по событию.
/// </summary>
public sealed class Order : Entity<Guid>, IAggregateRoot
{
    /// <summary>Максимальная длина названия товара в снимке позиции.</summary>
    public const int MaxItemNameLength = 200;

    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        OrderNumber number,
        string customerName,
        string customerEmail,
        string? customerPhone,
        string? comment,
        OrderContactChannel preferredChannel,
        OrderPriority priority,
        Guid? basketId,
        DateTimeOffset createdAt)
        : base(id == Guid.Empty ? Guid.NewGuid() : id)
    {
        Number = number;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CustomerPhone = customerPhone;
        Comment = comment;
        PreferredChannel = preferredChannel;
        Priority = priority;
        BasketId = basketId;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public OrderNumber Number { get; private set; } = default!;

    public OrderStatus Status { get; private set; }

    public string CustomerName { get; private set; } = default!;

    public string CustomerEmail { get; private set; } = default!;

    public string? CustomerPhone { get; private set; }

    /// <summary>Комментарий клиента к заявке.</summary>
    public string? Comment { get; private set; }

    /// <summary>Комментарий менеджера, которым сопровождается смена статуса.</summary>
    public string? ManagerComment { get; private set; }

    public OrderContactChannel PreferredChannel { get; private set; }

    public OrderPriority Priority { get; private set; }

    /// <summary>
    /// Корзина, из которой оформлена заявка (ADR 0005): «что гость набрал» остаётся
    /// в истории заявки, даже если корзина уже очищена.
    /// </summary>
    public Guid? BasketId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    /// <summary>Суммарное количество единиц товара в заявке.</summary>
    public int TotalQuantity => _items.Sum(item => item.Quantity);

    public decimal TotalAmount => _items.Sum(item => item.LineTotal);

    /// <summary>Валюта заявки — валюта первой позиции: магазин работает в одной валюте.</summary>
    public string Currency => _items.Count == 0 ? Money.DefaultCurrency : _items[0].UnitPrice.Currency;

    /// <summary>Заявка в терминальном статусе: дальше менять нечего.</summary>
    public bool IsFinal => Status is OrderStatus.Completed or OrderStatus.Cancelled;

    /// <summary>
    /// Оформление заявки: позиции берутся снимком из корзины гостя, заявка сразу получает
    /// статус <see cref="OrderStatus.Pending"/> и номер, выданный генератором номера.
    /// </summary>
    public static Order Create(
        OrderNumber number,
        string customerName,
        string customerEmail,
        IEnumerable<OrderItem> items,
        string? customerPhone = null,
        string? comment = null,
        OrderContactChannel preferredChannel = OrderContactChannel.Email,
        OrderPriority priority = OrderPriority.Standard,
        Guid? basketId = null)
    {
        ArgumentNullException.ThrowIfNull(number);

        var itemList = items?.ToList() ?? [];

        if (itemList.Count == 0)
        {
            throw new ArgumentException("Заявка не может быть пустой.", nameof(items));
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Имя клиента обязательно.", nameof(customerName));
        }

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new ArgumentException("E-mail клиента обязателен.", nameof(customerEmail));
        }

        var order = new Order(
            Guid.NewGuid(),
            number,
            customerName.Trim(),
            customerEmail.Trim(),
            Normalize(customerPhone),
            Normalize(comment),
            preferredChannel,
            priority,
            basketId == Guid.Empty ? null : basketId,
            DateTimeOffset.UtcNow);

        order._items.AddRange(itemList);

        return order;
    }

    /// <summary>
    /// Смена статуса менеджером. Разрешены только шаги «вперёд» по воронке
    /// (<see cref="CanTransitionTo"/>). Повторная установка того же статуса — ошибка: иначе
    /// клиент получил бы два одинаковых письма по одному переходу.
    /// </summary>
    public Result ChangeStatus(OrderStatus newStatus, string? managerComment = null)
    {
        if (newStatus == Status)
        {
            return Result.Failure(Error.Conflict(
                "order.status.already_set",
                $"Заявка уже в статусе '{Status}'."));
        }

        if (!CanTransitionTo(newStatus))
        {
            return Result.Failure(Error.Conflict(
                "order.status.transition_not_allowed",
                $"Из статуса '{Status}' нельзя перевести заявку в '{newStatus}'."));
        }

        Status = newStatus;
        ManagerComment = Normalize(managerComment);
        Touch();

        return Result.Success();
    }

    /// <summary>Допустимые переходы статусов (воронка заявки без онлайн-оплаты).</summary>
    public bool CanTransitionTo(OrderStatus newStatus) => Status switch
    {
        OrderStatus.Pending => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
        OrderStatus.Confirmed => newStatus is OrderStatus.InProgress or OrderStatus.Cancelled,
        OrderStatus.InProgress => newStatus is OrderStatus.Completed or OrderStatus.Cancelled,
        _ => false,
    };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
