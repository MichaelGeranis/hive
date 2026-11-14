using System.Net;
using Hive.Api.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hive.Tests.Api;

/// <summary>
/// The API is only for the desktop app on the same machine: it answers loopback host names only,
/// so a web page that points its own domain at 127.0.0.1 (DNS rebinding) is refused, and it will
/// not start outside Development with the public default credentials.
/// </summary>
public class LocalhostOnlyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalhostOnlyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("localhost:5002")]
    public async Task Request_WithLoopbackHost_IsServed(string host)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = host;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("attacker.example")]
    [InlineData("attacker.example:5002")]
    public async Task Request_WithAnyOtherHost_IsRefused(string host)
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = host;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void EnsureNotPublicDefault_DefaultsOutsideDevelopment_Throws()
    {
        var act = () => new AdminCredentials().EnsureNotPublicDefault(Environment(Environments.Production), NullLogger.Instance);

        act.Should().Throw<InvalidOperationException>().WithMessage("*admin/admin123*");
    }

    [Fact]
    public void EnsureNotPublicDefault_DefaultsInDevelopment_IsAllowed()
    {
        var act = () => new AdminCredentials().EnsureNotPublicDefault(Environment(Environments.Development), NullLogger.Instance);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNotPublicDefault_RandomPasswordOutsideDevelopment_IsAllowed()
    {
        var credentials = new AdminCredentials { Password = "f3a9c1d27be84e6d" };

        var act = () => credentials.EnsureNotPublicDefault(Environment(Environments.Production), NullLogger.Instance);

        act.Should().NotThrow();
    }

    private static IHostEnvironment Environment(string name) =>
        Mock.Of<IHostEnvironment>(e => e.EnvironmentName == name);
}
