using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Todo.Application.Todos;
using Todo.Domain;
using Todo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Config (ADR-0024): non-secret values from appsettings/ConfigMap, secrets from env
//     vars sourced by the K8s Secret (ADR-0005) — nothing hardcoded here either way.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

// --- DB (ADR-0004, ADR-0023): EnableRetryOnFailure for transient faults only — this
//     must NEVER retry a 409 optimistic-concurrency conflict, which is an application
//     decision surfaced to the client, not a transient fault. EF Core's retry strategy
//     only engages on real transport/connection failures, so this boundary holds
//     automatically — nothing extra needed to enforce it.
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        npgsql.CommandTimeout(10); // short — every query here is fast CRUD (ADR-0022)
    }));

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<TodoService>();

// --- Errors: built-in ASP.NET Core ProblemDetails (RFC 9457), not hand-rolled
//     middleware — this is native since .NET 8, use the framework rather than
//     reinventing it.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<Todo.Api.Middleware.TodoExceptionHandler>();

// --- Observability (ADR-0018): OpenTelemetry SDK now, console exporter for now —
//     backend/dashboard choice is deferred, not the instrumentation itself.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("todo-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddConsoleExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter());

builder.Logging.AddOpenTelemetry(o => o.AddConsoleExporter());

builder.Services.AddControllers();

var app = builder.Build();

// Local dev convenience only — production migrations run via a Helm pre-upgrade hook Job
// (ADR-0004), specifically to avoid multiple replicas racing to migrate concurrently. This
// path only ever runs in Development (docker-compose), never in the deployed environment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })); // Step 4 skeleton; see
                                                                  // ADR-0023 re: deepening
                                                                  // this to check DB
                                                                  // connectivity in Step 6
app.MapControllers();

app.Run();

public partial class Program { } // exposed for Todo.IntegrationTests' WebApplicationFactory