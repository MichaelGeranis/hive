using FluentAssertions;
using Hive.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using System.Text.Encodings.Web;

namespace Hive.Tests.Api.Authentication;

/// <summary>
/// Tests for BasicAuthenticationHandler.
/// These tests verify credential parsing, Base64 decoding, and auth validation behaviors.
/// </summary>
public class BasicAuthenticationHandlerTests
{
    private const string ValidUsername = "admin";
    private const string ValidPassword = "admin123";

    // ============== AdminCredentials Tests ==============

    [Fact]
    public void AdminCredentials_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var credentials = new AdminCredentials();

        // Assert
        credentials.Username.Should().Be("admin");
        credentials.Password.Should().Be("admin123");
    }

    [Fact]
    public void AdminCredentials_CanSetCustomValues()
    {
        // Arrange & Act
        var credentials = new AdminCredentials
        {
            Username = "custom-admin",
            Password = "secure-password-123"
        };

        // Assert
        credentials.Username.Should().Be("custom-admin");
        credentials.Password.Should().Be("secure-password-123");
    }

    // ============== Base64 Encoding Tests ==============

    [Fact]
    public void Base64Encoding_ValidCredentials_CanBeDecoded()
    {
        // Arrange
        var rawCredentials = $"{ValidUsername}:{ValidPassword}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

        // Act
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var parts = decoded.Split(':', 2);

        // Assert
        parts.Should().HaveCount(2);
        parts[0].Should().Be(ValidUsername);
        parts[1].Should().Be(ValidPassword);
    }

    [Fact]
    public void Base64Encoding_PasswordWithColons_PreservesFullPassword()
    {
        // Arrange - password containing colon (common edge case)
        var username = "admin";
        var password = "pass:word:with:colons";
        var rawCredentials = $"{username}:{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

        // Act
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var parts = decoded.Split(':', 2);

        // Assert
        parts.Should().HaveCount(2);
        parts[0].Should().Be(username);
        parts[1].Should().Be(password); // colons preserved
    }

    [Fact]
    public void Base64Encoding_SpecialCharacters_AreHandledCorrectly()
    {
        // Arrange
        var username = "admin";
        var password = "p@ssw0rd!#$%";
        var rawCredentials = $"{username}:{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

        // Act
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        var parts = decoded.Split(':', 2);

        // Assert
        parts[0].Should().Be(username);
        parts[1].Should().Be(password);
    }

    // ============== Handler Construction Tests ==============

    [Fact]
    public async Task Handler_WithValidCredentials_Authenticates()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = CreateHttpContext(ValidUsername, ValidPassword);
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity!.Name.Should().Be(ValidUsername);
    }

    [Fact]
    public async Task Handler_WithInvalidPassword_FailsAuthentication()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = CreateHttpContext(ValidUsername, "wrongpassword");
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task Handler_WithInvalidUsername_FailsAuthentication()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = CreateHttpContext("wronguser", ValidPassword);
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_WithMissingAuthorizationHeader_FailsAuthentication()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Contain("Missing Authorization header");
    }

    [Fact]
    public async Task Handler_WithNonBasicScheme_FailsAuthentication()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer some-token";
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Contain("Invalid Authorization scheme");
    }

    [Fact]
    public async Task Handler_WithInvalidBase64_FailsAuthentication()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Basic !!!not-valid-base64!!!";
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should().Contain("Invalid Base64 encoding");
    }

    [Fact]
    public async Task Handler_WithValidCredentials_GrantsAdminRole()
    {
        // Arrange
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = CreateHttpContext(ValidUsername, ValidPassword);
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Principal!.IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public async Task Handler_WithMissingColonInCredentials_FailsAuthentication()
    {
        // Arrange - credentials encoded without colon separator
        var handler = CreateHandler(ValidUsername, ValidPassword);
        var context = new DefaultHttpContext();
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("nocredentialseparator"));
        context.Request.Headers.Authorization = $"Basic {encoded}";
        await handler.InitializeAsync(new AuthenticationScheme("Basic", null, typeof(BasicAuthenticationHandler)), context);

        // Act
        var result = await handler.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    // ============== Helper Methods ==============

    private static BasicAuthenticationHandler CreateHandler(string username, string password)
    {
        var credentials = new AdminCredentials { Username = username, Password = password };
        var credentialsOptions = Options.Create(credentials);
        var optionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        optionsMonitor.Setup(o => o.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());

        var loggerFactory = new Mock<ILoggerFactory>();
        var logger = new Mock<ILogger<BasicAuthenticationHandler>>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        return new BasicAuthenticationHandler(
            optionsMonitor.Object,
            loggerFactory.Object,
            UrlEncoder.Default,
            credentialsOptions);
    }

    private static HttpContext CreateHttpContext(string username, string password)
    {
        var context = new DefaultHttpContext();
        var rawCredentials = $"{username}:{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));
        context.Request.Headers.Authorization = $"Basic {encoded}";
        return context;
    }
}
