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
