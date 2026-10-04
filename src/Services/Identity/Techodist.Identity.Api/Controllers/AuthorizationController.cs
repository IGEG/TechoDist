using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Techodist.Identity.Application.Features.Auth.Commands.AuthenticateAdmin;
using Techodist.Identity.Application.Features.Users.Queries.GetAdminProfile;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Techodist.Identity.Api.Controllers;

/// <summary>
/// Token-endpoint OpenIddict. Запрос разбирает и валидирует сама библиотека (passthrough),
/// контроллер получает готовый <see cref="OpenIddictRequest"/> и выпускает токены:
/// <list type="bullet">
/// <item><c>grant_type=password</c> — вход администратора в админ-панель;</item>
/// <item><c>grant_type=refresh_token</c> — продление сессии с перечитыванием профиля из БД.</item>
/// </list>
/// </summary>
[ApiController]
public sealed class AuthorizationController(ISender sender) : ControllerBase
{
    [HttpPost("~/connect/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange(CancellationToken cancellationToken)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Не удалось прочитать OpenID Connect-запрос.");

        if (request.IsPasswordGrantType())
        {
            var authentication = await sender.Send(
                new AuthenticateAdminCommand(request.Username ?? string.Empty, request.Password ?? string.Empty),
                cancellationToken);

            return authentication.IsSuccess
                ? IssueTokens(
                    authentication.Value.Id,
                    authentication.Value.Email,
                    authentication.Value.DisplayName,
                    authentication.Value.Roles,
                    request.GetScopes())
                : InvalidGrant(authentication.Error.Message);
        }

        if (request.IsRefreshTokenGrantType())
        {
            // Principal refresh-токена содержит sub: профиль перечитываем из БД, чтобы отключённая
            // учётная запись перестала получать access-токены, а смена ролей применялась сразу.
            var info = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (!Guid.TryParse(info.Principal?.FindFirst(Claims.Subject)?.Value, out var userId))
            {
                return InvalidGrant("Refresh-токен не содержит идентификатор администратора.");
            }

            var profile = await sender.Send(new GetAdminProfileQuery(userId), cancellationToken);

            if (profile.IsFailure)
            {
                return InvalidGrant("Учётная запись администратора не найдена.");
            }

            if (!profile.Value.IsActive)
            {
                return InvalidGrant("Учётная запись отключена администратором.");
            }

            return IssueTokens(
                profile.Value.Id,
                profile.Value.Email,
                profile.Value.DisplayName,
                profile.Value.Roles,
                request.GetScopes());
        }

        return BadRequest(new OpenIddictResponse
        {
            Error = Errors.UnsupportedGrantType,
            ErrorDescription = "Поддерживаются только потоки password и refresh_token.",
        });
    }

    private IActionResult IssueTokens(
        Guid userId,
        string email,
        string displayName,
        IReadOnlyList<string> roles,
        IEnumerable<string> scopes)
        => SignIn(
            CreatePrincipal(userId, email, displayName, roles, scopes),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

    /// <summary>Собирает principal администратора с claim'ами, которые попадут в access-токен.</summary>
    private static ClaimsPrincipal CreatePrincipal(
        Guid userId,
        string email,
        string displayName,
        IReadOnlyList<string> roles,
        IEnumerable<string> scopes)
    {
        // nameType/roleType совпадают с настройкой JwtBearer в BuildingBlocks.Web
        // (NameClaimType = "name", RoleClaimType = "role"), поэтому [Authorize(Roles=...)] работает.
        var identity = new ClaimsIdentity(Schemes.Bearer, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, userId.ToString());
        identity.SetClaim(Claims.Name, displayName);
        identity.SetClaim(Claims.Email, email);
        identity.SetClaims(Claims.Role, [.. roles]);

        // Запрошенные scope определяют аудиторию (aud) access-токена; offline_access — выдачу refresh-токена.
        identity.SetScopes(scopes);
        identity.SetDestinations(GetDestinations);

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Определяет, какие claim'ы попадут в access-токен: только они могут быть прочитаны админ-панелью
    /// (access-токен подписан, но не зашифрован). Refresh-токен всегда шифруется и хранит все claim'ы,
    /// поэтому <c>sub</c> в нём доступен даже при пустом списке destination.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
        => claim.Type switch
        {
            Claims.Subject or Claims.Name or Claims.Email or Claims.Role => [Destinations.AccessToken],
            _ => [],
        };

    /// <summary>Ответ по спецификации OAuth 2.0: тело с <c>error=invalid_grant</c> формирует OpenIddict.</summary>
    private IActionResult InvalidGrant(string description)
        => Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
            }),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
