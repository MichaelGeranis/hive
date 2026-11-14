using Hive.Api.Authentication;
using Hive.Application;
using Hive.Infrastructure;
using Hive.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Configure Admin Credentials.
// The compiled-in default (admin/admin123) is a well-known local-development
// convenience only. Override it via HIVE_ADMIN_USERNAME / HIVE_ADMIN_PASSWORD
// (or the standard AdminCredentials__Username / AdminCredentials__Password
// environment variables) for any instance reachable outside localhost.
builder.Services.Configure<AdminCredentials>(options =>
{
    builder.Configuration.GetSection("AdminCredentials").Bind(options);

    var envUsername = Environment.GetEnvironmentVariable("HIVE_ADMIN_USERNAME");
    if (!string.IsNullOrEmpty(envUsername))
    {
        options.Username = envUsername;
    }

    var envPassword = Environment.GetEnvironmentVariable("HIVE_ADMIN_PASSWORD");
    if (!string.IsNullOrEmpty(envPassword))
    {
        options.Password = envPassword;
    }
});

// Answer loopback host names only, so a web page that points its own domain at 127.0.0.1 (DNS
// rebinding) is refused. Set in code: the desktop app starts the backend from an arbitrary working
// directory, where appsettings.json is not found, and this must hold regardless.
builder.Services.PostConfigure<HostFilteringOptions>(options =>
    options.AllowedHosts = ["localhost", "127.0.0.1", "[::1]"]);

// Configure Authentication
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

builder.Services.AddAuthorization();

// Add Application Layer Services (Use Cases)
builder.Services.AddApplicationServices();

// Add Infrastructure Layer Services (Repositories, DbContext)
// Use SQLite in production, in-memory for development
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase", defaultValue: builder.Environment.IsDevelopment());

if (useInMemory)
{
    builder.Services.AddInfrastructureServices(seedData: false);
}
else
{
    var connectionString = $"Data Source={GetDatabasePath()}";
    // The desktop app passes the key from the OS keystore; without one the database is unencrypted
    var databaseKey = Environment.GetEnvironmentVariable(DatabaseEncryption.KeyVariable);
    if (string.IsNullOrEmpty(databaseKey))
    {
        databaseKey = null;
    }
    else if (!DatabaseEncryption.IsValidKey(databaseKey))
    {
        throw new InvalidOperationException($"{DatabaseEncryption.KeyVariable} must be 64 hexadecimal characters.");
    }

    builder.Services.AddSqliteInfrastructureServices(connectionString, seedData: true, databaseKey: databaseKey);
}

static string GetDatabasePath()
{
    // Check for explicit path in environment variable
    var explicitPath = Environment.GetEnvironmentVariable("HIVE_DATABASE_PATH");
    if (!string.IsNullOrEmpty(explicitPath))
    {
        return explicitPath;
    }

    // Determine platform-specific user data folder
    string appDataFolder;
    if (OperatingSystem.IsMacOS())
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        appDataFolder = Path.Combine(home, "Library", "Application Support", "Hive");
    }
    else if (OperatingSystem.IsWindows())
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        appDataFolder = Path.Combine(appData, "Hive");
    }
    else
    {
        // Linux/other: use ~/.local/share/Hive
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        appDataFolder = Path.Combine(home, ".local", "share", "Hive");
    }

    // Ensure directory exists
    Directory.CreateDirectory(appDataFolder);

    return Path.Combine(appDataFolder, "hive.db");
}

// Add Controllers
builder.Services.AddControllers();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hive - Engineering Manager API",
        Version = "v1",
        Description = "API for managing engineering teams, tasks, and projects.",
        Contact = new OpenApiContact
        {
            Name = "Engineering Team",
            Email = "engineering@company.com"
        }
    });

    // Add Basic Authentication to Swagger
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Basic Authentication header using the Bearer scheme. Enter your username and password."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("basic", document)] = []
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// The well-known default admin credentials are public. The desktop app replaces them with a
// random password on every launch, so outside Development they mean something is wrong.
app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminCredentials>>().Value
    .EnsureNotPublicDefault(app.Environment, app.Logger);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hive API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Electron to verify backend is running
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

app.Run();

/// <summary>The API's entry point, public so integration tests can host it.</summary>
public partial class Program;
