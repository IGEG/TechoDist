namespace Techodist.Notification.Application.Email;

/// <summary>
/// Готовое к отправке письмо. Тема и тело формируются шаблонами, отправитель берётся
/// из настроек SMTP, поэтому в письме только получатель и содержимое.
/// </summary>
public sealed record EmailMessage(string To, string Subject, string Body);
