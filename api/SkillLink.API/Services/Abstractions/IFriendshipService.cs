// Services/Abstractions/IFriendshipService.cs
using SkillLink.API.Models; // adjust if your User type is in a different namespace

namespace SkillLink.API.Services.Abstractions
{
    public interface IFriendshipService
    {
        List<User> GetFollowers(int userId);
        void Follow(int followerId, int followedId);
        void Unfollow(int followerId, int followedId);
        List<User> GetMyFriends(int userId);
        List<User> SearchUsers(string query, int currentUserId);
    }
}
