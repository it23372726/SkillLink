namespace SkillLink.API.Models
{
    public class Skill
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsPredefined { get; set; }
    }

    public class UserSkill
    {
        public int UserSkillId { get; set; }
        public int UserId { get; set; }
        public int SkillId { get; set; }
        public string Level { get; set; } = string.Empty;
        public Skill? Skill { get; set; } // navigation
    }

    public class AddSkillRequest
    {
        public int UserId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Level { get; set; } = "Beginner";
    }
}
