using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[Route("api/[controller]")]
[ApiController]
public class RequestsController : ControllerBase
{
    private readonly RequestService _service;
    private readonly AcceptedRequestService _acceptedRequestService;

    public RequestsController(RequestService service, AcceptedRequestService acceptedRequestService)
    {
        _service = service;
        _acceptedRequestService = acceptedRequestService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAllRequests());
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string q)
    {
        if (string.IsNullOrEmpty(q))
        {
            return BadRequest(new { message = "Search query is required" });
        }
        
        var results = _service.SearchRequests(q);
        return Ok(results);
    }

    [HttpGet("by-requestId/{id}")]
    public IActionResult GetById(int id)
    {
        var req = _service.GetById(id);
        if (req == null)
        {
            return NotFound(new { message = "Request not found" });
        }
        return Ok(req);
    }

    [HttpGet("by-learnerId/{id}")]
    public IActionResult GetByLearnerId(int id)
    {
        var req = _service.GetByLearnerId(id);
        if (req == null || req.Count == 0)
        {
            return NotFound(new { message = "No requests found for this user" });
        }
        return Ok(req);
    }

    [HttpPost]
    public IActionResult Create(Request req)
    {
        _service.AddRequest(req);
        return Ok(new { message = "Request Created" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateRequest(int id, [FromBody] Request req)
    {
        var existingRequest = _service.GetById(id);
        if (existingRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        _service.UpdateRequest(id, req);
        return Ok(new { message = "Request updated" });
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateStatus(int id, [FromBody] string status)
    {
        var existingRequest = _service.GetById(id);
        if (existingRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        _service.UpdateStatus(id, status);
        return Ok(new { message = "Status updated" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var existingRequest = _service.GetById(id);
        if (existingRequest == null)
        {
            return NotFound(new { message = "Request not found" });
        }

        _service.DeleteRequest(id);
        return Ok(new { message = "Request deleted" });
    }


    // accepting area

    [HttpPost("{id}/accept")]
    public IActionResult AcceptRequest(int id)
    {
        try
        {
            // Get user ID from claims (assuming you're using JWT authentication)
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            _acceptedRequestService.AcceptRequest(id, userId);
            return Ok(new { message = "Request accepted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("accepted")]
    public IActionResult GetAcceptedRequests()
    {
        try
        {
            // Get user ID from claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var acceptedRequests = _acceptedRequestService.GetAcceptedRequestsByUser(userId);
            return Ok(acceptedRequests);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/accepted-status")]
    public IActionResult GetAcceptedStatus(int id)
    {
        try
        {
            // Get user ID from claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var hasAccepted = _acceptedRequestService.HasUserAcceptedRequest(userId, id);
            return Ok(new { hasAccepted });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("accepted/{id}/schedule")]
    public IActionResult ScheduleMeeting(int id, [FromBody] ScheduleMeetingRequest request)
    {
        try
        {
            _acceptedRequestService.ScheduleMeeting(id, request.ScheduleDate, request.MeetingType, request.MeetingLink);
            return Ok(new { message = "Meeting scheduled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("accepted/requester")]
    public IActionResult GetRequestsIAskedFor()
    {
        try
        {
            // Get user ID from claims
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            
            var requested = _acceptedRequestService.GetRequestsIAskedFor(userId);
            return Ok(requested);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}