using Techodist.Notification.Application.Abstractions;
using Techodist.Notification.Application.Email;

namespace Techodist.Notification.UnitTests.Fakes;

/// <summary>
/// Подменный отправитель писем: складывает письма в список в порядке отправки
/// и умеет падать по требованию — так проверяется, что отметка об обработке
/// сообщения ставится только после успешной отправки.
/// </summary>
internal sealed class FakeEmailSender : IEmailSender
{
    private readonly List<EmailMessage> _sent = [];

    public IReadOnlyList<EmailMessage> Sent => _sent;

    public Exception? Failure { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        _sent.Add(message);

        return Task.CompletedTask;
    }
}
