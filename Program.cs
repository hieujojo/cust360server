using CRM.Api.Infrastructure.Extensions;
using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules;
using CRM.Api.Modules.Repositories;
using CRM.Api.Modules.Services;
using CRM.Api.Services;
using CRM.Api.Shared.Extensions;
using CRM.Api.Shared.Middleware;
using Serilog;

// Load .env file vào Environment Variables
DotNetEnv.Env.Load();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.Configure<FirebaseSettings>(
    builder.Configuration.GetSection(FirebaseSettings.SectionName)
);
builder.Services.AddSingleton<FirebaseService>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<NotificationService>();

builder
    .Services.AddMongoDb(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddEmail(builder.Configuration)
    .AddCurrentUser()
    .AddRoleBasedAuthorization()
    .AddDefaultCors(builder.Configuration)
    .AddSwagger()
    .AddControllers();

builder
    .Services.AddIdentityModule()
    .AddCustomerModule()
    .AddSalesModule()
    .AddActivityModule(builder.Configuration);

builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Kiểm tra kết nối MongoDB
await app.Services.TestMongoDbConnectionAsync();

await app.Services.EnsureIdentityIndexesAsync();
await app.Services.EnsureCustomerIndexesAsync();
await app.Services.EnsureSalesIndexesAsync();
await app.Services.EnsureActivityIndexesAsync();

app.UseMiddleware<ErrorHandlerMiddleware>();

if (app.Environment.IsDevelopment())
    app.UseSwaggerUi();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<OrgResolverMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
