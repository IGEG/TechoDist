namespace Techodist.Order.Domain.Enums;

/// <summary>
/// Статус заявки. Онлайн-оплаты нет, поэтому «жизненный цикл» — это работа менеджера
/// с обращением клиента, а не оплата: новая → подтверждена → в работе → выполнена.
/// </summary>
public enum OrderStatus
{
    /// <summary>Заявка оформлена гостем и ждёт менеджера.</summary>
    Pending = 0,

    /// <summary>Менеджер подтвердил заявку (связался с клиентом).</summary>
    Confirmed = 1,

    /// <summary>Заявка в работе (комплектация, доставка, монтаж).</summary>
    InProgress = 2,

    /// <summary>Заявка выполнена — терминальный статус.</summary>
    Completed = 3,

    /// <summary>Заявка отменена — терминальный статус.</summary>
    Cancelled = 4,
}
