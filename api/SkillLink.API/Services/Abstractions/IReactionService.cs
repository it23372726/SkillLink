namespace SkillLink.API.Services.Abstractions
{
    public interface IReactionService
    {
        (int likes, int dislikes, string? my) GetReactionSummary(int userId, string postType, int postId);
        void UpsertReaction(int userId, string postType, int postId, string reactionType);
        void RemoveReaction(int userId, string postType, int postId);
    }
}
