using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackRepository _feedbackRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly CurrentUser _currentUser;

    public FeedbackController(
        IFeedbackRepository feedbackRepo,
        ICustomerRepository customerRepo,
        CurrentUser currentUser)
    {
        _feedbackRepo = feedbackRepo;
        _customerRepo = customerRepo;
        _currentUser = currentUser;
    }

    // ─── GET /api/feedback ────────────────────────────────────────────────────

    /// <summary>
    /// Lấy danh sách feedback có phân trang và filter.
    /// Query params: type, category, status, sortBy, sortDir, page, pageSize
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedFeedbackResponse>> GetFeedbacks(
        [FromQuery] string? type = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _feedbackRepo.FindPagedAsync(
            type, category, status, sortBy, sortDir, page, pageSize, ct);

        var dtos = items.Select(MapToDTO).ToList();

        var response = new PagedFeedbackResponse
        {
            Items = dtos,
            Pagination = new PaginationMeta
            {
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };

        return Ok(response);
    }

    // ─── GET /api/feedback/{id} ───────────────────────────────────────────────

    /// <summary>Lấy chi tiết một feedback theo ID.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<FeedbackDTO>> GetFeedbackById(string id, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.FindByIdAsync(id, ct);
        if (feedback == null)
            return NotFound(new { message = "Feedback không tồn tại" });

        return Ok(MapToDTO(feedback));
    }

    // ─── POST /api/feedback ───────────────────────────────────────────────────

    /// <summary>Tạo feedback mới.</summary>
    [HttpPost]
    public async Task<ActionResult<FeedbackDTO>> CreateFeedback(
        [FromBody] CreateFeedbackRequest request,
        CancellationToken ct = default)
    {
        // Validate customer if type = customer
        if (request.Type == "customer" && !string.IsNullOrEmpty(request.CustomerId))
        {
            var customer = await _customerRepo.FindByIdAsync(request.CustomerId, ct);
            if (customer == null)
                return BadRequest(new { message = "Customer không tồn tại" });
        }

        var feedback = new Feedback
        {
            type = request.Type,
            category = request.Category,
            title = request.Title,
            content = request.Content,
            status = "open",
            isAnonymous = request.IsAnonymous,
            authorId = _currentUser.UserId,
            authorName = request.IsAnonymous ? "Người dùng ẩn danh" : _currentUser.DisplayName,
            authorEmail = request.IsAnonymous ? null : _currentUser.Email,
            customerId = request.CustomerId,
            customerName = null, // TODO: populate from customer if needed
            replies = [],
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _feedbackRepo.InsertAsync(feedback, ct);

        return CreatedAtAction(
            nameof(GetFeedbackById),
            new { id = feedback.id },
            MapToDTO(feedback));
    }

    // ─── POST /api/feedback/{id}/reply ────────────────────────────────────────

    /// <summary>Thêm reply vào feedback.</summary>
    [HttpPost("{id}/reply")]
    public async Task<ActionResult> AddReply(
        string id,
        [FromBody] CreateReplyRequest request,
        CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.FindByIdAsync(id, ct);
        if (feedback == null)
            return NotFound(new { message = "Feedback không tồn tại" });

        var reply = new FeedbackReply
        {
            feedbackId = id,
            content = request.Content,
            authorId = _currentUser.UserId,
            authorName = request.IsAnonymous ? "Người dùng ẩn danh" : _currentUser.DisplayName,
            isAnonymous = request.IsAnonymous,
            createdAt = DateTime.UtcNow
        };

        var success = await _feedbackRepo.AddReplyAsync(id, reply, ct);
        if (!success)
            return BadRequest(new { message = "Không thể thêm reply" });

        return Ok(new { message = "Đã thêm reply thành công" });
    }

    // ─── PATCH /api/feedback/{id}/status ──────────────────────────────────────

    /// <summary>Cập nhật trạng thái feedback.</summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateFeedbackStatusRequest request,
        CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.FindByIdAsync(id, ct);
        if (feedback == null)
            return NotFound(new { message = "Feedback không tồn tại" });

        var success = await _feedbackRepo.UpdateStatusAsync(id, request.Status, ct);
        if (!success)
            return BadRequest(new { message = "Không thể cập nhật trạng thái" });

        return Ok(new { message = "Đã cập nhật trạng thái thành công" });
    }

    // ─── DELETE /api/feedback/{id} ────────────────────────────────────────────

    /// <summary>Xóa mềm feedback.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFeedback(string id, CancellationToken ct = default)
    {
        var feedback = await _feedbackRepo.FindByIdAsync(id, ct);
        if (feedback == null)
            return NotFound(new { message = "Feedback không tồn tại" });

        var success = await _feedbackRepo.SoftDeleteAsync(id, ct);
        if (!success)
            return BadRequest(new { message = "Không thể xóa feedback" });

        return Ok(new { message = "Đã xóa feedback thành công" });
    }

    // ─── Helper methods ───────────────────────────────────────────────────────

    private static FeedbackDTO MapToDTO(Feedback feedback)
    {
        return new FeedbackDTO
        {
            Id = feedback.id,
            Type = feedback.type,
            Category = feedback.category,
            Title = feedback.title,
            Content = feedback.content,
            Status = feedback.status,
            IsAnonymous = feedback.isAnonymous,
            AuthorId = feedback.authorId,
            AuthorName = feedback.authorName,
            AuthorEmail = feedback.authorEmail,
            CustomerId = feedback.customerId,
            CustomerName = feedback.customerName,
            Replies = feedback.replies.Select(r => new FeedbackReplyDTO
            {
                Id = r.id,
                FeedbackId = r.feedbackId,
                Content = r.content,
                AuthorId = r.authorId,
                AuthorName = r.authorName,
                IsAnonymous = r.isAnonymous,
                CreatedAt = r.createdAt
            }).ToList(),
            CreatedAt = feedback.createdAt,
            UpdatedAt = feedback.updatedAt
        };
    }
}
