namespace SkillLink.API.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public int FollowerId { get; set; }   // who follows
        public int FollowedId { get; set; }   // who is followed
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
