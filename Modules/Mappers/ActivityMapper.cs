using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class ActivityMapper
{
    public static ActivityResponse ToResponse(this Activity activity, User? creator = null)
        => new()
        {
            Id = activity.id,
            CustomerId = activity.customerId,
            DealId = activity.dealId,
            Type = activity.type,
            Source = activity.source,
            IsAutoSync = activity.isAutoSync,
            CreatedBy = activity.createdBy,
            CreatedByName = creator?.displayName ?? (activity.source == ActivitySources.System ? "Hệ thống" : string.Empty),
            OccurredAt = activity.occurredAt,
            CreatedAt = activity.createdAt,
            Outcome = activity.outcome,
            DurationMinutes = activity.durationMinutes,
            Note = activity.note,
            Subject = activity.subject,
            Summary = activity.summary,
            Direction = activity.direction,
            Location = activity.location,
            Attendees = activity.attendees,
            NextSteps = activity.nextSteps,
            Body = activity.body,
            SystemEvent = activity.systemEvent,
            Metadata = activity.metadata
        };
}
