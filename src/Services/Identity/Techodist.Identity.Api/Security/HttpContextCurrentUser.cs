using Techodist.Identity.Application.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Techodist.Identity.Api.Security;

/// <summary>
/// Текущий администратор из claims запроса. JwtBearer настроен с <c>MapInboundClaims = false</c>,
/// поэтому claim'ы читаются в исходном виде (<c>sub</c> и <c>email</c>), как их выпустил OpenIddict.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
        => Guid.TryParse(accessor.HttpContext?.User.FindFirst(Claims.Subject)?.Value, out var userId)
            ? userId
            : null;

    public string? Email => accessor.HttpContext?.User.FindFirst(Claims.Email)?.Value;
}
