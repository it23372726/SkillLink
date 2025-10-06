namespace SkillLink.API.Repositories.Abstractions
{
    public interface IReactionRepository
    {
        /// <summary>
        /// Insert or update user's reaction for a post (LIKE / DISLIKE).
        /// </summary>
        void Upsert(int userId, string postType, int postId, string reaction);

        /// <summary>
        /// Remove user's reaction for a post.
        /// </summary>
        void Remove(int userId, string postType, int postId);

        /// <summary>
        /// Returns (likes, dislikes) counts for a post.
        /// </summary>
        (int Likes, int Dislikes) GetCounts(string postType, int postId);

        /// <summary>
        /// Returns current user's reaction ("LIKE", "DISLIKE" or null).
        /// </summary>
        string? GetMyReaction(int userId, string postType, int postId);
    }
}
