using SkillLink.API.Models;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface ITutorPostRepository
    {
        // Create / image
        int Create(TutorPost post);
        void SetImageUrl(int postId, string imageUrl);

        // Reads
        List<TutorPostWithUser> GetAll();
        TutorPostWithUser? GetById(int postId);

        // Accept logic helpers
        bool IsParticipant(int postId, int userId);
        (int TutorId, int MaxParticipants, string Status, int Current)? GetPostMeta(int postId);
        void AddParticipant(int postId, int userId);
        void UpdateStatus(int postId, string status);

        // Schedule
        void Schedule(int postId, ScheduleTutorPostDto body);

        // Update / Delete helpers
        int? GetOwnerId(int postId);
        int GetCurrentParticipantsCount(int postId);
        void UpdatePost(int postId, string title, string? description, int maxParticipants);
        void Delete(int postId);
        HashSet<int> GetAcceptedPostIdsForUser(int userId, IEnumerable<int> postIds);

    }
}
