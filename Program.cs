using CRM.Api.Infrastructure.Extensions;
using CRM.Api.Modules;
using CRM.Api.Shared.Extensions;
using CRM.Api.Shared.Middleware;

// Load .env file vào Environment Variables
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMongoDb(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddEmail(builder.Configuration)
    .AddCurrentUser()
    .AddRoleBasedAuthorization()
    .AddDefaultCors(builder.Configuration)
    .AddSwagger()
    .AddControllers();

builder.Services
    .AddIdentityModule()
    .AddCustomerModule()
    .AddSalesModule();

builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

// Kiểm tra kết nối MongoDB
await app.Services.TestMongoDbConnectionAsync();

await app.Services.EnsureIdentityIndexesAsync();
await app.Services.EnsureCustomerIndexesAsync();
await app.Services.EnsureSalesIndexesAsync();

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
