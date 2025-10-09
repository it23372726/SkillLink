using SkillLink.API.Models;

namespace SkillLink.API.Services.Abstractions
{
    public interface ITutorPostService
    {
        int CreatePost(TutorPost post);
        void SetImageUrl(int postId, string imageUrl);
        List<TutorPostWithUser> GetPosts();
        TutorPostWithUser? GetById(int postId);
        void AcceptPost(int postId, int userId);
        void Schedule(int postId, ScheduleTutorPostDto body);
        void UpdatePost(int postId, int tutorId, UpdateTutorPostDto dto);
        void DeletePost(int postId, int tutorId);
        bool HasUserAccepted(int postId, int userId);
        IDictionary<int, bool> GetAcceptedMapForUser(IEnumerable<int> postIds, int userId);
    }
}
