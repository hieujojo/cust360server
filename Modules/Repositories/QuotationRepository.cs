using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class QuotationRepository : BaseRepository<Quotation>, IQuotationRepository
{
    public QuotationRepository(MongoDbContext context, CurrentUser currentUser)
        : base(context, "quotations", currentUser)
    {
    }

    public async Task<List<Quotation>> FindAsync(FilterDefinition<Quotation> additionalFilter, SortDefinition<Quotation>? sort = null, CancellationToken ct = default)
        => await FindManyWithDepartmentScopeAsync(additionalFilter, sort: sort, ct: ct);

    public async Task<string?> GetLatestQuotationCodeAsync(int year, CancellationToken ct = default)
    {
        var prefix = $"QUO-{year}-";
        var filter = Builders<Quotation>.Filter.Regex(x => x.code, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
        
        var sort = Builders<Quotation>.Sort.Descending(x => x.code);
        
        var latest = await Collection.Find(filter).Sort(sort).FirstOrDefaultAsync(ct);
        return latest?.code;
    }
}
