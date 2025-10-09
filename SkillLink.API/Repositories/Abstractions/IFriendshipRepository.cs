using SkillLink.API.Models;
using SkillLink.API.Dtos.Auth;

namespace SkillLink.API.Repositories.Abstractions
{
    public interface IFriendshipRepository
    {
        List<User> GetFollowersBasic(int userId);
        bool IsFollowing(int followerId, int followedId);
        void InsertFollow(int followerId, int followedId);
        void DeleteFollow(int followerId, int followedId);
        List<User> GetMyFriendsBasic(int userId);
        List<User> SearchUsersBasic(string query, int currentUserId);
        Task<List<UserSummaryDto>> GetFollowersAsync(int userId);
        Task<List<UserSummaryDto>> GetFollowingAsync(int userId);
    }
}
