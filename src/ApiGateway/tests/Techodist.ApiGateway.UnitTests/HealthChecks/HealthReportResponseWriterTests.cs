using System.Text.Json;
using Techodist.ApiGateway.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Techodist.ApiGateway.UnitTests.HealthChecks;

public sealed class HealthReportResponseWriterTests
{
    [Fact]
    public async Task WriteAsync_WritesStatusAndEveryCheckAsJson()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["identity-api"] = new(
                    HealthStatus.Healthy,
                    "200 http://localhost:5102/health/live",
                    TimeSpan.FromMilliseconds(12),
                    exception: null,
                    data: new Dictionary<string, object>()),
                ["catalog-api"] = new(
                    HealthStatus.Unhealthy,
                    "503 http://localhost:5101/health/live",
                    TimeSpan.FromMilliseconds(5),
                    exception: null,
                    data: new Dictionary<string, object>()),
            },
            TimeSpan.FromMilliseconds(17));

        await HealthReportResponseWriter.WriteAsync(context, report);

        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        var root = await ReadResponseAsync(context);
        Assert.Equal("Unhealthy", root.GetProperty("status").GetString());
        Assert.Equal(17, root.GetProperty("totalDurationMs").GetInt32());

        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        Assert.Equal(2, checks.Count);
        Assert.Contains(checks, check =>
            check.GetProperty("name").GetString() == "catalog-api"
            && check.GetProperty("status").GetString() == "Unhealthy"
            && check.GetProperty("description").GetString() == "503 http://localhost:5101/health/live");
        Assert.Contains(checks, check =>
            check.GetProperty("name").GetString() == "identity-api"
            && check.GetProperty("durationMs").GetInt32() == 12);
    }

    [Fact]
    public async Task WriteAsync_WithoutChecks_ReportsHealthy()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await HealthReportResponseWriter.WriteAsync(
            context,
            new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero));

        var root = await ReadResponseAsync(context);

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Empty(root.GetProperty("checks").EnumerateArray());
    }

    private static async Task<JsonElement> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        using var document = await JsonDocument.ParseAsync(context.Response.Body);

        return document.RootElement.Clone();
    }
}
