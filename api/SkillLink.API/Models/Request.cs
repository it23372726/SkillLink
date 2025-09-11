public class Request
{
    public int RequestId { get; set; }
    public int LearnerId { get; set; }
    public string SkillName { get; set; }
    public string? Topic {get; set;}
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAt { get; set; }
    public string? Description {get; set;}
}
// Add to your Models namespace
public class RequestWithUser : Request
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
public class AcceptedRequest
{
    public int AcceptedRequestId { get; set; }
    public int RequestId { get; set; }
    public int AcceptorId { get; set; }
    public DateTime AcceptedAt { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime? ScheduleDate { get; set; }
    public string MeetingType { get; set; } = string.Empty;
    public string MeetingLink { get; set; } = string.Empty;
}

public class AcceptedRequestWithDetails : AcceptedRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public int RequesterId { get; set; }
}

public class ScheduleMeetingRequest
{
    public DateTime ScheduleDate { get; set; }
    public string MeetingType { get; set; } = string.Empty;
    public string MeetingLink { get; set; } = string.Empty;
}
