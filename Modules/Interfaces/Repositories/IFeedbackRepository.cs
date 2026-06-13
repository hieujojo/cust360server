using CRM.Api.Modules.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Interfaces.Repositories;

public interface IFeedbackRepository
{
    // ─── CRUD ─────────────────────────────────────────────────────────────────
    Task<Feedback?> FindByIdAsync(string id, CancellationToken ct = default);
    Task InsertAsync(Feedback feedback, CancellationToken ct = default);
    Task UpdateAsync(string id, UpdateDefinition<Feedback> update, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(string id, CancellationToken ct = default);

    // ─── List & Pagination ────────────────────────────────────────────────────
    Task<(List<Feedback> Items, long Total)> FindPagedAsync(
        string? type,
        string? category,
        string? status,
        string sortBy,
        string sortDir,
        int page,
        int pageSize,
        CancellationToken ct = default);

    // ─── Reply Operations ─────────────────────────────────────────────────────
    Task<bool> AddReplyAsync(string feedbackId, FeedbackReply reply, CancellationToken ct = default);

    // ─── Status Operations ────────────────────────────────────────────────────
    Task<bool> UpdateStatusAsync(string id, string status, CancellationToken ct = default);

    // ─── Indexes ──────────────────────────────────────────────────────────────
    Task EnsureIndexesAsync(CancellationToken ct = default);
}
