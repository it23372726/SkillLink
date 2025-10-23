namespace SkillLink.API.Models.Reports
{
    public class SkillDemandRow
    {
        public string SkillName { get; set; } = "";
        public int TotalRequests { get; set; }
        public int Scheduled { get; set; }
        public int Completed { get; set; }
        public DateTime? FirstRequestAt { get; set; }
        public DateTime? LastRequestAt { get; set; }
    }
}
