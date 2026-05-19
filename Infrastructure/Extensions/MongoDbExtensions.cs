using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Infrastructure.Settings;

namespace CRM.Api.Infrastructure.Extensions;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MongoDbSettings>(config.GetSection(MongoDbSettings.SectionName));
        services.AddSingleton<MongoDbContext>();
        return services;
    }

    /// <summary>Kiểm tra kết nối MongoDB và hiển thị thông báo.</summary>
    public static async Task TestMongoDbConnectionAsync(this IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<MongoDbContext>>();
        var mongoContext = services.GetRequiredService<MongoDbContext>();
        var settings = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>();

        try
        {
            logger.LogInformation("🔌 Đang kết nối MongoDB...");
            
            var isConnected = await mongoContext.TestConnectionAsync();
            
            if (isConnected)
            {
                var dbName = settings.Value.DatabaseName;
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Database: {dbName}");
                Console.ResetColor();
                
                logger.LogInformation("✅ MongoDB connected successfully to database '{DatabaseName}'", dbName);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ FAILED: Không thể kết nối MongoDB!");
                Console.ResetColor();
                
                logger.LogError("❌ MongoDB connection failed");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("❌ ERROR: Lỗi khi kết nối MongoDB!");
            Console.WriteLine($"   {ex.Message}");
            Console.ResetColor();
            
            logger.LogError(ex, "❌ MongoDB connection error");
        }
    }
}
