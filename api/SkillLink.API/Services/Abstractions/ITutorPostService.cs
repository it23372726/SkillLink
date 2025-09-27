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
        void Schedule(int postId, DateTime scheduledAt);
        void UpdatePost(int postId, int tutorId, UpdateTutorPostDto dto);
        void DeletePost(int postId, int tutorId);
    }
}
