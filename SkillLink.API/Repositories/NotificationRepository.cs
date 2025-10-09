using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

public class NotificationRepository : INotificationRepository
{
    private readonly DbHelper _db;

    public NotificationRepository(DbHelper db) => _db = db;

    public void Insert(Notification n)
    {
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = new SqlCommand(@"
            INSERT INTO Notifications (UserId, [Type], [Title], [Body], [Link], IsRead)
            VALUES (@uid, @type, @title, @body, @link, 0)", conn);
        cmd.Parameters.AddWithValue("@uid", n.UserId);
        cmd.Parameters.AddWithValue("@type", n.Type);
        cmd.Parameters.AddWithValue("@title", n.Title);
        cmd.Parameters.AddWithValue("@body", (object?)n.Body ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@link", (object?)n.Link ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public List<Notification> ListForUser(int userId)
    {
        var list = new List<Notification>();
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = new SqlCommand(@"
            SELECT NotificationId, UserId, [Type], [Title], [Body], [Link], IsRead, CreatedAt
            FROM Notifications
            WHERE UserId=@uid
            ORDER BY CreatedAt DESC", conn);
        cmd.Parameters.AddWithValue("@uid", userId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Notification
            {
                NotificationId = r.GetInt32(0),
                UserId = r.GetInt32(1),
                Type = r.GetString(2),
                Title = r.GetString(3),
                Body = r.IsDBNull(4) ? null : r.GetString(4),
                Link = r.IsDBNull(5) ? null : r.GetString(5),
                IsRead = r.GetBoolean(6),
                CreatedAt = r.GetDateTime(7)
            });
        }
        return list;
    }

    public void MarkRead(int userId, int notificationId)
    {
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = new SqlCommand(@"
            UPDATE Notifications SET IsRead=1 WHERE NotificationId=@id AND UserId=@uid", conn);
        cmd.Parameters.AddWithValue("@id", notificationId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.ExecuteNonQuery();
    }

    public void MarkAllRead(int userId)
    {
        using var conn = _db.GetConnection();
        conn.Open();
        var cmd = new SqlCommand(@"UPDATE Notifications SET IsRead=1 WHERE UserId=@uid", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.ExecuteNonQuery();
    }
}
