using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

/// <summary>
/// Full-text search service sử dụng MongoDB Atlas Search hoặc regex fallback.
/// </summary>
public sealed class AtlasSearchService
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly CurrentUser _currentUser;

    public AtlasSearchService(
        MongoDbContext context,
        CurrentUser currentUser,
        ILogger<AtlasSearchService> logger
    )
    {
        _collection = context.GetCollection<BsonDocument>("customers");
        _currentUser = currentUser;
    }

    public async Task<CustomerSearchResponse> SearchAsync(
        string query,
        int limit = 50,
        CancellationToken ct = default
    )
    {
        try
        {
            // Thử dùng $search (Atlas Search) trước
            var searchStage = new BsonDocument(
                "$search",
                new BsonDocument
                {
                    ["index"] = "customer_search",
                    ["compound"] = new BsonDocument
                    {
                        ["must"] = new BsonArray
                        {
                            new BsonDocument(
                                "text",
                                new BsonDocument
                                {
                                    ["query"] = query,
                                    ["path"] = new BsonArray
                                    {
                                        "name",
                                        "email",
                                        "phone",
                                        "customerCode",
                                    },
                                    ["fuzzy"] = new BsonDocument("maxEdits", 1),
                                }
                            ),
                            new BsonDocument(
                                "equals",
                                new BsonDocument
                                {
                                    ["path"] = "organizationId",
                                    // organizationId được lưu dạng string (không phải ObjectId)
                                    ["value"] = _currentUser.OrganizationId,
                                }
                            ),
                        },
                        ["filter"] = new BsonArray
                        {
                            new BsonDocument(
                                "equals",
                                new BsonDocument { ["path"] = "isDeleted", ["value"] = false }
                            ),
                        },
                    },
                }
            );

            // Department scoping cho Role 3
            if (_currentUser.Role == 3 && !string.IsNullOrEmpty(_currentUser.DepartmentId))
            {
                var compound = searchStage["$search"]["compound"].AsBsonDocument;
                compound["filter"]
                    .AsBsonArray.Add(
                        new BsonDocument(
                            "equals",
                            new BsonDocument
                            {
                                ["path"] = "departmentId",
                                // departmentId được lưu dạng string (ObjectId string)
                                ["value"] = _currentUser.DepartmentId,
                            }
                        )
                    );
            }

            var addFieldsStage = new BsonDocument(
                "$addFields",
                new BsonDocument("score", new BsonDocument("$meta", "searchScore"))
            );
            var limitStage = new BsonDocument("$limit", limit);

            var pipeline = new[] { searchStage, addFieldsStage, limitStage };
            var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync(ct);
            if (results.Count == 0)
            {
                // Atlas Search index/analyzer có thể không match được (hoặc chưa được tạo).
                // Để đảm bảo tính đúng đắn cho feature search, fallback sang regex nếu rỗng.
                return await FallbackRegexSearchAsync(query, limit, ct);
            }

            var responseItems = results
                .Select(doc => new CustomerSearchResultResponse
                {
                    Id = doc["_id"].AsObjectId.ToString(),
                    CustomerCode = doc.GetValue("customerCode", "").AsString,
                    Name = doc.GetValue("name", "").AsString,
                    Status = doc.GetValue("status", "").AsString,
                    Email = doc.GetValue("email", BsonNull.Value).IsBsonNull
                        ? null
                        : doc["email"].AsString,
                    Phone = doc.GetValue("phone", BsonNull.Value).IsBsonNull
                        ? null
                        : doc["phone"].AsString,
                    Score = (float)doc["score"].AsDouble,
                })
                .ToList();

            return new CustomerSearchResponse
            {
                Results = responseItems,
                TotalCount = responseItems.Count,
                Query = query,
            };
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("$search"))
        {
            return await FallbackRegexSearchAsync(query, limit, ct);
        }
    }

    private async Task<CustomerSearchResponse> FallbackRegexSearchAsync(
        string query,
        int limit,
        CancellationToken ct
    )
    {
        var escapedQuery = System.Text.RegularExpressions.Regex.Escape(query);
        var regex = new BsonRegularExpression(escapedQuery, "i");

        // organizationId/departmentId trong collection customers là string
        var filter =
            Builders<BsonDocument>.Filter.Eq("organizationId", _currentUser.OrganizationId)
            & Builders<BsonDocument>.Filter.Eq("isDeleted", false)
            & Builders<BsonDocument>.Filter.Or(
                Builders<BsonDocument>.Filter.Regex("name", regex),
                Builders<BsonDocument>.Filter.Regex("email", regex),
                Builders<BsonDocument>.Filter.Regex("phone", regex),
                Builders<BsonDocument>.Filter.Regex("customerCode", regex)
            );

        if (_currentUser.Role == 3 && !string.IsNullOrEmpty(_currentUser.DepartmentId))
        {
            filter &= Builders<BsonDocument>.Filter.Eq("departmentId", _currentUser.DepartmentId);
        }

        var results = await _collection.Find(filter).Limit(limit).ToListAsync(ct);

        var responseItems = results
            .Select(doc => new CustomerSearchResultResponse
            {
                Id = doc["_id"].AsObjectId.ToString(),
                CustomerCode = doc.GetValue("customerCode", "").AsString,
                Name = doc.GetValue("name", "").AsString,
                Status = doc.GetValue("status", "").AsString,
                Email = doc.GetValue("email", BsonNull.Value).IsBsonNull
                    ? null
                    : doc["email"].AsString,
                Phone = doc.GetValue("phone", BsonNull.Value).IsBsonNull
                    ? null
                    : doc["phone"].AsString,
                Score = 1.0f, // Không có text score trong regex
            })
            .ToList();

        return new CustomerSearchResponse
        {
            Results = responseItems,
            TotalCount = responseItems.Count,
            Query = query,
        };
    }
}
