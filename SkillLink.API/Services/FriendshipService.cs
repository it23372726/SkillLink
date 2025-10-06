using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _repo;

        public FriendshipService(IFriendshipRepository repo)
        {
            _repo = repo;
        }

        public List<User> GetFollowers(int userId) =>
            _repo.GetFollowersBasic(userId);

        public void Follow(int followerId, int followedId)
        {
            if (followerId == followedId)
                throw new InvalidOperationException("You cannot follow yourself.");

            if (_repo.IsFollowing(followerId, followedId))
                throw new InvalidOperationException("Already following");

            _repo.InsertFollow(followerId, followedId);
        }

        public void Unfollow(int followerId, int followedId) =>
            _repo.DeleteFollow(followerId, followedId);

        public List<User> GetMyFriends(int userId) =>
            _repo.GetMyFriendsBasic(userId);

        public List<User> SearchUsers(string query, int currentUserId) =>
            _repo.SearchUsersBasic(query, currentUserId);
    }
}
