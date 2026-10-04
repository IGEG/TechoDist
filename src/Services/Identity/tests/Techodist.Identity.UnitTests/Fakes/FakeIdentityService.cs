using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Dtos;

namespace Techodist.Identity.UnitTests.Fakes;

/// <summary>
/// Подменный <see cref="IIdentityService"/>: держит администраторов в памяти, запоминает
/// переданные аргументы и умеет возвращать заданную ошибку. Этого достаточно, чтобы проверить
/// логику обработчиков без ASP.NET Core Identity и БД (тот же приём, что FakeProductRepository в Catalog).
/// </summary>
internal sealed class FakeIdentityService : IIdentityService
{
    private readonly Dictionary<Guid, AdminUserDto> _users = [];

    /// <summary>Если задана — аутентификация и операции изменения возвращают эту ошибку.</summary>
    public Error? Failure { get; set; }

    /// <summary>Результат успешной аутентификации; если не задан — собирается из переданного e-mail.</summary>
    public AuthenticatedAdminDto? AuthenticatedAdmin { get; set; }

    public (string Email, string Password)? LastCredentials { get; private set; }

    public AdminUserDto? LastCreated { get; private set; }

    public (Guid UserId, string DisplayName, IReadOnlyCollection<string> Roles)? LastUpdated { get; private set; }

    public (Guid UserId, bool IsActive)? LastStatusChange { get; private set; }

    public (Guid UserId, string NewPassword)? LastPasswordReset { get; private set; }

    public (Guid UserId, string CurrentPassword, string NewPassword)? LastPasswordChange { get; private set; }

    public (int Page, int PageSize, string? Search)? LastListQuery { get; private set; }

    public void Seed(params AdminUserDto[] users)
    {
        foreach (var user in users)
        {
            _users[user.Id] = user;
        }
    }

    public Task<Result<AuthenticatedAdminDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        LastCredentials = (email, password);

        if (Failure is not null)
        {
            return Task.FromResult(Result.Failure<AuthenticatedAdminDto>(Failure));
        }

        if (AuthenticatedAdmin is null && _users.Values.FirstOrDefault(u => u.Email == email) is { } seeded)
        {
            AuthenticatedAdmin = new AuthenticatedAdminDto(seeded.Id, seeded.Email, seeded.DisplayName, seeded.Roles);
        }

        return Task.FromResult(Result.Success(
            AuthenticatedAdmin ?? new AuthenticatedAdminDto(Guid.NewGuid(), email, email, [])));
    }

    public Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        LastPasswordChange = (userId, currentPassword, newPassword);

        return Task.FromResult(Failure is null ? Result.Success() : Result.Failure(Failure));
    }

    public Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        LastPasswordReset = (userId, newPassword);

        return Task.FromResult(Failure is null ? Result.Success() : Result.Failure(Failure));
    }

    public Task<AdminUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

    public Task<PagedResult<AdminUserDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        LastListQuery = (page, pageSize, search);

        var matches = _users.Values
            .Where(u => string.IsNullOrWhiteSpace(search)
                        || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.Email)
            .ToList();

        return Task.FromResult(PagedResult<AdminUserDto>.Create(matches, matches.Count, page, pageSize));
    }

    public Task<Result<Guid>> CreateAsync(
        string email,
        string displayName,
        string password,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (Failure is not null)
        {
            return Task.FromResult(Result.Failure<Guid>(Failure));
        }

        var created = new AdminUserDto(
            Guid.NewGuid(),
            email,
            displayName,
            [.. roles],
            IsActive: true,
            CreatedAt: DateTimeOffset.UtcNow,
            LastLoginAt: null,
            IsLockedOut: false);

        _users[created.Id] = created;
        LastCreated = created;

        return Task.FromResult(Result.Success(created.Id));
    }

    public Task<Result> UpdateAsync(
        Guid userId,
        string displayName,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        LastUpdated = (userId, displayName, roles);

        if (Failure is not null)
        {
            return Task.FromResult(Result.Failure(Failure));
        }

        if (_users.TryGetValue(userId, out var user))
        {
            _users[userId] = user with { DisplayName = displayName, Roles = [.. roles] };
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        LastStatusChange = (userId, isActive);

        if (Failure is not null)
        {
            return Task.FromResult(Result.Failure(Failure));
        }

        if (_users.TryGetValue(userId, out var user))
        {
            _users[userId] = user with { IsActive = isActive };
        }

        return Task.FromResult(Result.Success());
    }
}
