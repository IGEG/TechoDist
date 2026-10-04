using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Techodist.Notification.Application.Features.OrderSubmitted;
using Techodist.Notification.Application.Options;
using Techodist.Notification.UnitTests.Fakes;
using Xunit;

namespace Techodist.Notification.UnitTests.Application;

/// <summary>
/// Реакция на событие «заявка оформлена»: письмо магазину, подтверждение клиенту и
/// идемпотентность по MessageId (ADR 0003). Отметка об обработке ставится только после
/// успешной отправки, иначе повтор доставки мог бы потерять письмо.
/// </summary>
public sealed class OrderSubmittedEmailHandlerTests
{
    private static readonly Guid MessageId = Guid.Parse("33333333-4444-5555-6666-777777777777");

    private readonly FakeEmailSender _emails = new();

    private readonly FakeProcessedMessageStore _processedMessages = new();

    [Fact]
    public async Task Handle_SendsStoreEmailFirstThenCustomerConfirmation()
    {
        await Handler().HandleAsync(NotificationTestData.SubmittedEvent(), MessageId, CancellationToken.None);

        Assert.Equal(2, _emails.Sent.Count);
        Assert.Equal(NotificationTestData.StoreEmail, _emails.Sent[0].To);
        Assert.Equal(NotificationTestData.CustomerEmail, _emails.Sent[1].To);
        Assert.Equal(MessageId, Assert.Single(_processedMessages.Processed));
    }

    [Fact]
    public async Task Handle_DuplicateMessage_DoesNotSendEmailsAgain()
    {
        var handler = Handler();

        await handler.HandleAsync(NotificationTestData.SubmittedEvent(), MessageId, CancellationToken.None);
        await handler.HandleAsync(NotificationTestData.SubmittedEvent(), MessageId, CancellationToken.None);

        Assert.Equal(2, _emails.Sent.Count);
        Assert.Equal(1, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_WhenSendingFails_MessageIsNotMarkedProcessed()
    {
        _emails.Failure = new InvalidOperationException("SMTP недоступен.");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Handler().HandleAsync(NotificationTestData.SubmittedEvent(), MessageId, CancellationToken.None));

        Assert.Empty(_emails.Sent);
        Assert.Equal(0, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_WithoutMessageId_SendsEmailsButSkipsJournal()
    {
        await Handler().HandleAsync(NotificationTestData.SubmittedEvent(), messageId: null, CancellationToken.None);

        Assert.Equal(2, _emails.Sent.Count);
        Assert.Equal(0, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_CustomerConfirmationDisabled_SendsOnlyStoreEmail()
    {
        await Handler(sendCustomerConfirmation: false)
            .HandleAsync(NotificationTestData.SubmittedEvent(), MessageId, CancellationToken.None);

        var sent = Assert.Single(_emails.Sent);

        Assert.Equal(NotificationTestData.StoreEmail, sent.To);
        Assert.Equal(1, _processedMessages.MarkCalls);
    }

    [Fact]
    public async Task Handle_NullMessage_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Handler().HandleAsync(null!, MessageId, CancellationToken.None));
    }

    private OrderSubmittedEmailHandler Handler(bool sendCustomerConfirmation = true) => new(
        _emails,
        _processedMessages,
        Options.Create(new NotificationOptions
        {
            StoreEmail = NotificationTestData.StoreEmail,
            StoreName = "Techodist",
            SendCustomerConfirmation = sendCustomerConfirmation,
        }),
        NullLogger<OrderSubmittedEmailHandler>.Instance);
}
