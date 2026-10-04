using Techodist.Identity.Application.Abstractions;

namespace Techodist.Identity.UnitTests.Fakes;

/// <summary>Подменный <see cref="ICurrentUser"/>: позволяет проверить защиту от «самоблокировки».</summary>
internal sealed class FakeCurrentUser(Guid? userId = null, string? email = null) : ICurrentUser
{
    public Guid? UserId { get; set; } = userId;

    public string? Email { get; set; } = email;
}
