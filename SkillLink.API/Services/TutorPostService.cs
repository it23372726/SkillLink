using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class TutorPostService : ITutorPostService
    {
        private readonly ITutorPostRepository _repo;

        public TutorPostService(ITutorPostRepository repo)
        {
            _repo = repo;
        }

        /* ---------- Create ---------- */
        public int CreatePost(TutorPost post)
        {
            return _repo.Create(post);
        }

        public void SetImageUrl(int postId, string imageUrl)
        {
            _repo.SetImageUrl(postId, imageUrl);
        }

        /* ---------- Read (All with Tutor + CurrentParticipants) ---------- */
        public List<TutorPostWithUser> GetPosts() => _repo.GetAll();

        /* ---------- Read by Id ---------- */
        public TutorPostWithUser? GetById(int postId) => _repo.GetById(postId);

        /* ---------- Accept ---------- */
        public void AcceptPost(int postId, int userId)
        {
            // Duplicate check
            if (_repo.IsParticipant(postId, userId))
                throw new InvalidOperationException("You already accepted this post.");

            // Load meta
            var meta = _repo.GetPostMeta(postId);
            if (meta is null) throw new KeyNotFoundException("Post not found.");

            var (tutorId, max, status, current) = meta.Value;

            // Self-accept check
            if (tutorId == userId)
                throw new InvalidOperationException("You cannot accept your own post.");

            // Full check
            bool isFull = (max <= 0) || (current >= max);
            if (isFull)
                throw new InvalidOperationException("Post is full.");

            // Status checks
            if (string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Post is already closed.");
            if (string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Post is already scheduled.");

            // Add participant
            _repo.AddParticipant(postId, userId);

            // If it just became full, close it
            if (current + 1 >= max)
            {
                _repo.UpdateStatus(postId, "Closed");
            }
        }

        /* ---------- Schedule ---------- */
        public void Schedule(int postId, ScheduleTutorPostDto body)
        {
            _repo.Schedule(postId, body);
        }

        /* ---------- Update (Owner only) ---------- */
        public void UpdatePost(int postId, int tutorId, UpdateTutorPostDto dto)
        {
            var ownerId = _repo.GetOwnerId(postId);
            if (ownerId is null) throw new KeyNotFoundException("Post not found.");
            if (ownerId.Value != tutorId)
                throw new UnauthorizedAccessException("You can only update your own posts.");

            var currentCount = _repo.GetCurrentParticipantsCount(postId);
            if (dto.MaxParticipants < currentCount)
                throw new InvalidOperationException(
                    $"MaxParticipants ({dto.MaxParticipants}) cannot be less than current participants ({currentCount}).");

            _repo.UpdatePost(postId, dto.Title, dto.Description, dto.MaxParticipants);

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                _repo.UpdateStatus(postId, dto.Status);
            }
        }

        /* ---------- Delete (Owner only) ---------- */
        public void DeletePost(int postId, int tutorId)
        {
            var ownerId = _repo.GetOwnerId(postId);
            if (ownerId is null) throw new KeyNotFoundException("Post not found.");
            if (ownerId.Value != tutorId)
                throw new UnauthorizedAccessException("You can only delete your own posts.");

            _repo.Delete(postId);
        }

        public bool HasUserAccepted(int postId, int userId)
    {
        // Ensure post exists; keeps parity with controller 404 behavior if you want
        var meta = _repo.GetPostMeta(postId);
        if (meta is null) throw new KeyNotFoundException("Post not found.");

        return _repo.IsParticipant(postId, userId);
    }

    public IDictionary<int, bool> GetAcceptedMapForUser(IEnumerable<int> postIds, int userId)
    {
        var ids = postIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0) return new Dictionary<int, bool>();

        var acceptedIds = _repo.GetAcceptedPostIdsForUser(userId, ids); // HashSet<int>
        var map = new Dictionary<int, bool>(ids.Count);
        foreach (var id in ids)
            map[id] = acceptedIds.Contains(id);
        return map;
    }
    }
}
