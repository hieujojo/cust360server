using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Options;
using CRM.Api.Infrastructure.Settings;

namespace CRM.Api.Infrastructure.MongoDB;

/// <summary>Registry tập trung cho MongoDB collections.</summary>
public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDbContext(IOptions<MongoDbSettings> options)
    {
        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);
        
        // Cấu hình TLS/SSL cho MongoDB Atlas - tương thích Windows
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                                  System.Security.Authentication.SslProtocols.Tls13
        };
        
        // Tăng timeout cho kết nối ban đầu
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
        settings.ConnectTimeout = TimeSpan.FromSeconds(30);
        
        // Retry logic cho kết nối
        settings.RetryWrites = true;
        settings.RetryReads = true;
        
        _client = new MongoClient(settings);
        _database  = _client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
        => _database.GetCollection<T>(collectionName);

    /// <summary>Kiểm tra kết nối MongoDB.</summary>
    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), 
                cancellationToken: ct
            );
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Lấy thông tin server MongoDB.</summary>
    public async Task<string> GetServerInfoAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _database.RunCommandAsync<BsonDocument>(
                new BsonDocument("buildInfo", 1),
                cancellationToken: ct
            );
            var version = result["version"].AsString;
            return version;
        }
        catch
        {
            return "Unknown";
        }
    }
}
