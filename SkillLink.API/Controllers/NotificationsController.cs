using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Services.Abstractions;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _svc;
    public NotificationsController(INotificationService svc) => _svc = svc;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public IActionResult List() => Ok(_svc.ListForUser(GetUserId()));

    [HttpPost("{id}/read")]
    public IActionResult MarkRead(int id)
    {
        _svc.MarkRead(GetUserId(), id);
        return Ok(new { message = "ok" });
    }

    [HttpPost("read-all")]
    public IActionResult MarkAllRead()
    {
        _svc.MarkAllRead(GetUserId());
        return Ok(new { message = "ok" });
    }
}
