using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.ApiGateway.Extensions;

/// <summary>
/// Обработка <c>X-Forwarded-*</c>: в production перед шлюзом стоит Nginx Ingress,
/// поэтому реальный IP клиента (нужный rate-limit'у и логам) приходит в заголовках.
/// </summary>
public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddGatewayForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Списки доверенных прокси/сетей очищаем: адреса Ingress в кластере заранее
            // неизвестны, а сам шлюз наружу не публикуется (только через Ingress).
            options.KnownProxies.Clear();
            options.KnownNetworks.Clear();
        });

        return services;
    }
}
