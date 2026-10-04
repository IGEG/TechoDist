namespace Techodist.Identity.Api.Contracts;

/// <summary>Тело запроса на изменение профиля администратора (идентификатор берётся из маршрута).</summary>
public sealed record UpdateAdminUserRequest(string DisplayName, IReadOnlyCollection<string> Roles);

/// <summary>Тело запроса на включение/отключение учётной записи администратора.</summary>
public sealed record SetAdminUserStatusRequest(bool IsActive);

/// <summary>Тело запроса на сброс пароля администратора другим администратором.</summary>
public sealed record ResetAdminUserPasswordRequest(string NewPassword);

/// <summary>Тело запроса на смену собственного пароля (нужен текущий пароль).</summary>
public sealed record ChangeOwnPasswordRequest(string CurrentPassword, string NewPassword);
