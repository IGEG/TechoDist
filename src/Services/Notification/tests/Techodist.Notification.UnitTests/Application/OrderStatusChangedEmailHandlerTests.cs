using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Techodist.Notification.Application.Features.OrderStatusChanged;
using Techodist.Notification.Application.Options;
using Techodist.Notification.UnitTests.Fakes;
using Xunit;

namespace Techodist.Notification.UnitTests.Application;

/// <summary>
/// Реакция на событие «статус заявки изменён»: клиент получает письмо с новым статусом,
/// повторная доставка того же MessageId письмо не дублирует (ADR 0003).
/// </summary>
public sealed class OrderStatusChangedEmailHandlerTests
{
    private static readonly Guid MessageId = Guid.Parse("55555555-6666-7777-8888-999999999999");

    private readonly FakeEmailSender _emails = new();

    private readonly FakeProcessedMessageStore _processedMessages = new();

    [Fact]
    public async Task Handle_SendsStatusEmailToCustomer()
    {
        await Handler().HandleAsync(NotificationTestData.StatusChangedEvent(), MessageId, CancellationToken.None);

        var sent = Assert.Single(_emails.Sent);

        Assert.Equal(NotificationTestData.CustomerEmail, sent.To);
        Assert.Contains("подтверждена", sent.Subject);
        Assert.Contains("Согласовали сроки поставки.", sent.Body);
        Assert.Equal(MessageId, Assert.Single(_processedMessages.Processed));
    }

    [Fact]
    public async Task Handle_DuplicateMessage_DoesNotSendEmailAgain()
    {
        var handler = Handler();

        await handler.HandleAsync(NotificationTestData.StatusChangedEvent(), MessageId, CancellationToken.None);
        await handler.HandleAsync(NotificationTestData.StatusChangedEvent(), MessageId, CancellationToken.None);

        Assert.Single(_emails.Sent);
        Assert.Equal(1, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_WhenSendingFails_MessageIsNotMarkedProcessed()
    {
        _emails.Failure = new InvalidOperationException("SMTP недоступен.");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler().HandleAsync(NotificationTestData.StatusChangedEvent(), MessageId, CancellationToken.None));

        Assert.Empty(_emails.Sent);
        Assert.Equal(0, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_WithoutMessageId_SendsEmailButSkipsJournal()
    {
        await Handler().HandleAsync(NotificationTestData.StatusChangedEvent(), messageId: null, CancellationToken.None);

        Assert.Single(_emails.Sent);
        Assert.Equal(0, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_NullMessage_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Handler().HandleAsync(null!, MessageId, CancellationToken.None));
    }

    private OrderStatusChangedEmailHandler Handler() => new(
        _emails,
        _processedMessages,
        Options.Create(new NotificationOptions { StoreName = "Techodist" }),
        NullLogger<OrderStatusChangedEmailHandler>.Instance);
}
