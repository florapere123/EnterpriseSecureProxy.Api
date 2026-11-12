using System.Reflection;
using System.Threading.RateLimiting;
using EnterpriseSecureProxy.Application.Steps;
using EnterpriseSecureProxy.Application.UseCases;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using EnterpriseSecureProxy.Infrastructure.DependencyInjection;
using EnterpriseSecureProxy.Application.Auditing;
using EnterpriseSecureProxy.Api.Infrastructure;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Absolute request-size floor applied before any per-operation logic (bound to Kestrel too).
builder.Services.Configure<EnterpriseSecureProxy.Api.Infrastructure.RequestLimitsOptions>(
    builder.Configuration.GetSection(EnterpriseSecureProxy.Api.Infrastructure.RequestLimitsOptions.SectionName));
var requestLimits = builder.Configuration
    .GetSection(EnterpriseSecureProxy.Api.Infrastructure.RequestLimitsOptions.SectionName)
    .Get<EnterpriseSecureProxy.Api.Infrastructure.RequestLimitsOptions>()
    ?? new EnterpriseSecureProxy.Api.Infrastructure.RequestLimitsOptions();
// OWASP Security Misconfiguration: cap request body size in Kestrel before any handler runs.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = requestLimits.MaxGlobalBodyBytes);

// MVC controllers for the proxy API surface.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<ProxyHeadersOperationFilter>();
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

// Application use cases (orchestration entry points).
builder.Services.AddScoped<EncryptionStep>();
builder.Services.AddScoped<IdempotencyStep>();
builder.Services.AddScoped<ExecuteProxyOperationHandler>();
builder.Services.AddScoped<ProxyAuditRecorder>();

// Order matters: the specific proxy/domain handler runs first; the global handler is the catch-all fallback.
builder.Services.AddExceptionHandler<ProxyExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Infrastructure: selects Mock vs SQL repository via the "DataSource" switch (Strategy Pattern),
// and registers the forwarder, audit sink, validator, and resilient HTTP client.
builder.Services.AddProxyInfrastructure(builder.Configuration);

// Global fixed-window rate limiting to protect downstream C4I services from abuse/overload.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

var app = builder.Build();

// Fail fast at startup if the configured data source is a not-yet-implemented strategy.
var selectedDataSource = DataSourceOptions.Parse(app.Configuration[DataSourceOptions.Key]);
app.Logger.LogInformation("EnterpriseSecureProxy starting with DataSource strategy: {DataSource}.", selectedDataSource);

if (selectedDataSource == DataSourceKind.Sql)
{
    using var migrationCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    await app.MigrateAndSeedProxyConfigurationAsync(migrationCts.Token);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseMiddleware<EnterpriseSecureProxy.Api.Infrastructure.OperationRequestSizeLimitMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program { }
