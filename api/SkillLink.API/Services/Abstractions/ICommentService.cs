using System.Collections.Generic;
using SkillLink.API.Models;

namespace SkillLink.API.Services.Abstractions
{
    public interface ICommentService
    {
        List<dynamic> GetComments(string postType, int postId);
        void Add(string postType, int postId, int userId, string content);
        void Delete(int commentId, int userId, bool isAdmin);
        int Count(string postType, int postId);
    }
}
