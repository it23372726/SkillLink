using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]



public class RequestsController : ControllerBase{
    private readonly RequestService _service;

    public RequestsController(RequestService service){
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll(){
        return Ok(_service.GetAllRequests());
    }
    [HttpGet("by-requestId/{id}")]
    public IActionResult GetById(int id){
        var req = _service.GetById(id);
        if (req == null)
        {
            return NotFound(new { message = "Request not found" });
        }
        return Ok(req);
    }
    [HttpGet("by-learnerId/{id}")]
    public IActionResult GetByLearnerId(int id){
        var req = _service.GetByLearnerId(id);
        if (req == null || req.Count == 0)
        {
            return NotFound(new { message = "Request not found" });
        }
        return Ok(req);
    }

    [HttpPost]
    public IActionResult create(Request req){
        _service.AddRequest(req);
        return Ok(new {message = "Request Created"});
    }

    [HttpPatch("{id}")]
    public IActionResult UpdateStatus(int id, [FromBody] string status)
    {
        _service.UpdateStatus(id, status);
        return Ok(new { message = "Status updated" });
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _service.DeleteRequest(id);
        return Ok(new { message = "Request deleted" });
    }
}