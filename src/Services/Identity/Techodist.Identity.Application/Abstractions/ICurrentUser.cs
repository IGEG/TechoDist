namespace Techodist.Identity.Application.Abstractions;

/// <summary>
/// Текущий аутентифицированный администратор (из JWT-claims запроса).
/// Нужен, чтобы приложение могло защититься от «самоблокировки» (отключение себя, снятие роли Admin с себя).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }
}
