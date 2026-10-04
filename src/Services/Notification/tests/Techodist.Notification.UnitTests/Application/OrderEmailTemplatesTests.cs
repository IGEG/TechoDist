using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Notification.Application.Templates;
using Techodist.Notification.UnitTests.Fakes;
using Xunit;

namespace Techodist.Notification.UnitTests.Application;

/// <summary>
/// Шаблоны писем: получатель, тема и содержимое. Шаблон — чистая функция, поэтому SMTP
/// и брокер здесь не нужны; проверяются суммы, контакты, локализованные статусы и
/// поведение на незаполненных полях заявки.
/// </summary>
public sealed class OrderEmailTemplatesTests
{
    [Fact]
    public void StoreNotification_GoesToStoreWithAmountInSubject()
    {
        var message = OrderEmailTemplates.StoreNotification(
            NotificationTestData.SubmittedEvent(),
            NotificationTestData.StoreEmail);

        Assert.Equal(NotificationTestData.StoreEmail, message.To);
        Assert.Equal("Новая заявка TD-20261004-00001 — 48500.00 ₽", message.Subject);
    }

    [Fact]
    public void StoreNotification_ContainsContactsItemsAndTotal()
    {
        var message = OrderEmailTemplates.StoreNotification(
            NotificationTestData.SubmittedEvent(),
            NotificationTestData.StoreEmail);

        Assert.Contains("Новая заявка TD-20261004-00001 от 04.10.2026 09:30.", message.Body);
        Assert.Contains("Покупатель: Иван Петров", message.Body);
        Assert.Contains("E-mail: ivan.petrov@example.com", message.Body);
        Assert.Contains("Телефон: +7 999 123-45-67", message.Body);
        Assert.Contains("Предпочтительный способ связи: телефон", message.Body);
        Assert.Contains("Срочность: срочная", message.Body);
        Assert.Contains("- Установка регенерации Р-100: 1 шт. × 45000.00 ₽ = 45000.00 ₽", message.Body);
        Assert.Contains("- Картридж угольный: 2 шт. × 1750.00 ₽ = 3500.00 ₽", message.Body);
        Assert.Contains("Итого: 48500.00 ₽", message.Body);
        Assert.Contains("Комментарий клиента: Позвонить после 18:00", message.Body);
    }

    [Fact]
    public void StoreNotification_EmptyOptionalFields_AreRenderedAsNotSpecified()
    {
        var message = OrderEmailTemplates.StoreNotification(
            NotificationTestData.SubmittedEvent(
                phone: null,
                comment: null,
                channel: OrderChannel.Unknown,
                priority: OrderPriority.Standard),
            NotificationTestData.StoreEmail);

        Assert.Contains("Телефон: не указан", message.Body);
        Assert.Contains("Предпочтительный способ связи: не указан", message.Body);
        Assert.Contains("Срочность: обычная", message.Body);
        Assert.DoesNotContain("Комментарий клиента", message.Body);
    }

    [Fact]
    public void StoreNotification_WithoutItems_MarksPositionsMissing()
    {
        var message = OrderEmailTemplates.StoreNotification(
            NotificationTestData.SubmittedEvent(items: [], totalAmount: 0m),
            NotificationTestData.StoreEmail);

        Assert.Contains("- позиции не указаны", message.Body);
        Assert.Contains("Итого: 0.00 ₽", message.Body);
    }

    [Fact]
    public void CustomerConfirmation_GoesToCustomerAndIsSignedWithStore()
    {
        var message = OrderEmailTemplates.CustomerConfirmation(
            NotificationTestData.SubmittedEvent(),
            "Techodist");

        Assert.Equal(NotificationTestData.CustomerEmail, message.To);
        Assert.Equal("Заявка TD-20261004-00001 принята", message.Subject);
        Assert.Contains("Здравствуйте, Иван Петров!", message.Body);
        Assert.Contains("Мы получили вашу заявку TD-20261004-00001 от 04.10.2026 09:30", message.Body);
        Assert.Contains("Итого: 48500.00 ₽", message.Body);
        Assert.Contains("Менеджер свяжется с вами по указанному контакту (телефон).", message.Body);
        Assert.Contains("С уважением,", message.Body);
        Assert.EndsWith("Techodist", message.Body);
    }

    [Fact]
    public void StatusChange_ContainsTransitionAndManagerComment()
    {
        var message = OrderEmailTemplates.StatusChange(
            NotificationTestData.StatusChangedEvent(),
            "Techodist");

        Assert.Equal(NotificationTestData.CustomerEmail, message.To);
        Assert.Equal("Заявка TD-20261004-00001: подтверждена", message.Subject);
        Assert.Contains(
            "Статус вашей заявки TD-20261004-00001 изменился: ожидает подтверждения → подтверждена (04.10.2026 12:05).",
            message.Body);
        Assert.Contains("Комментарий менеджера: Согласовали сроки поставки.", message.Body);
        Assert.EndsWith("Techodist", message.Body);
    }

    [Fact]
    public void StatusChange_WithoutManagerComment_OmitsTheLine()
    {
        var message = OrderEmailTemplates.StatusChange(
            NotificationTestData.StatusChangedEvent(managerComment: null),
            "Techodist");

        Assert.DoesNotContain("Комментарий менеджера", message.Body);
    }

    [Theory]
    [InlineData("Pending", "ожидает подтверждения")]
    [InlineData("confirmed", "подтверждена")]
    [InlineData("InProgress", "в работе")]
    [InlineData("Completed", "выполнена")]
    [InlineData("Cancelled", "отменена")]
    [InlineData("Archived", "Archived")]
    [InlineData("   ", "без изменений")]
    [InlineData(null, "без изменений")]
    public void StatusLabel_IsLocalizedWithRawValueFallback(string? status, string expected)
    {
        Assert.Equal(expected, OrderEmailTemplates.StatusLabel(status));
    }

    [Fact]
    public void StoreNotification_InvalidArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrderEmailTemplates.StoreNotification(null!, NotificationTestData.StoreEmail));

        Assert.Throws<ArgumentException>(() =>
            OrderEmailTemplates.StoreNotification(NotificationTestData.SubmittedEvent(), "   "));
    }

    [Fact]
    public void CustomerConfirmation_BlankStoreName_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            OrderEmailTemplates.CustomerConfirmation(NotificationTestData.SubmittedEvent(), string.Empty));
    }
}
