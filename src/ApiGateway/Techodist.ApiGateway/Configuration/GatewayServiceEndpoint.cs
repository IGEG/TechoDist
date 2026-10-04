namespace Techodist.ApiGateway.Configuration;

/// <summary>
/// Адрес сервиса для агрегированного health-check шлюза (секция <c>Gateway:Services</c>).
/// </summary>
public sealed record GatewayServiceEndpoint(string Name, string Url);
