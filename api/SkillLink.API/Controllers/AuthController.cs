using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Dtos.Auth;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("verify-email")]
        public IActionResult VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Missing token" });

            var ok = _authService.VerifyEmailByToken(token);
            if (!ok) return BadRequest(new { message = "Invalid or expired token" });

            return Ok(new { message = "Email verified successfully" });
        }

        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetUserProfile()
        {
            var user = _authService.CurrentUser(User);
            if (user == null)
                return Unauthorized(new { message = "Invalid token or user not logged in" });

            var profile = _authService.GetUserProfile(user.UserId);
            return Ok(profile);
        }

        [Authorize]
        [HttpPut("profile")]
        public IActionResult UpdateUserProfile(UpdateProfileRequest request)
        {
            var user = _authService.CurrentUser(User);
            if (user == null)
                return Unauthorized(new { message = "Invalid token or user not logged in" });

            var success = _authService.UpdateUserProfile(user.UserId, request);
            if (!success)
                return BadRequest(new { message = "Failed to update profile" });

            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize]
        [HttpPut("profile/photo")]
        public IActionResult UpdateProfilePhoto(IFormFile profilePicture, [FromServices] IWebHostEnvironment env)
        {
            var me = _authService.CurrentUser(User);
            if (me == null) return Unauthorized();

            if (profilePicture == null || profilePicture.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var uploads = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploads);
            var ext = Path.GetExtension(profilePicture.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploads, fileName);

            using (var fs = new FileStream(fullPath, FileMode.Create))
                profilePicture.CopyTo(fs);

            var rel = $"/uploads/profiles/{fileName}";
            _authService.UpdateProfilePicture(me.UserId, rel);

            return Ok(new { profilePicture = rel });
        }

        [Authorize]
        [HttpDelete("profile/photo")]
        public IActionResult DeleteProfilePhoto()
        {
            var me = _authService.CurrentUser(User);
            if (me == null) return Unauthorized();

            _authService.UpdateProfilePicture(me.UserId, null);
            return Ok(new { message = "Photo removed" });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var user = _authService.CurrentUser(User);
            if (user == null)
                return Unauthorized(new { message = "Invalid token or user not logged in" });

            return Ok(user);
        }

        [HttpGet("by-userId/{id}")]
        public IActionResult GetUserById(int id)
        {
            var req = _authService.GetUserById(id);
            if (req == null)
                return NotFound(new { message = "User not found" });

            return Ok(req);
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.FullName) ||
                    string.IsNullOrWhiteSpace(req.Email) ||
                    string.IsNullOrWhiteSpace(req.Password))
                {
                    return BadRequest(new { message = "Full name, email, and password are required." });
                }

                string? profilePicPath = null;
                if (req.ProfilePicture != null && req.ProfilePicture.Length > 0)
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                    Directory.CreateDirectory(uploads);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(req.ProfilePicture.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await req.ProfilePicture.CopyToAsync(stream);
                    }

                    profilePicPath = "/uploads/" + fileName;
                }

                _authService.Register(new RegisterRequest
                {
                    FullName = req.FullName,
                    Email = req.Email,
                    Password = req.Password,
                    Role = string.IsNullOrWhiteSpace(req.Role) ? "Learner" : req.Role,
                    ProfilePicturePath = profilePicPath
                });

                return Ok(new { message = "User registered successfully. Please check your email to verify your account." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex}");
                return StatusCode(500, new { message = "Registration failed. Please try again later." });
            }
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            var token = _authService.Login(req);
            if (token == null) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(new { token });
        }

        // You can keep this inline, it's a different name so no collision
        public class UpdateTeachModeRequest { public bool ReadyToTeach { get; set; } }

        [Authorize]
        [HttpPut("teach-mode")]
        public IActionResult UpdateTeachMode(UpdateTeachModeRequest req)
        {
            var me = _authService.CurrentUser(User);
            if (me == null) return Unauthorized();

            var ok = _authService.UpdateTeachMode(me.UserId, req.ReadyToTeach);
            if (!ok) return BadRequest(new { message = "Failed to update mode" });

            var profile = _authService.GetUserProfile(me.UserId);
            return Ok(new { message = "Updated", readyToTeach = profile.ReadyToTeach });
        }

        [Authorize]
        [HttpPut("active")]
        public IActionResult UpdateActive([FromBody] AuthUpdateActiveRequest req)
        {
            var me = _authService.CurrentUser(User);
            if (me == null) return Unauthorized(new { message = "Invalid token" });

            var ok = _authService.SetActive(me.UserId, req.IsActive);
            if (!ok) return BadRequest(new { message = "Failed to update account status" });

            return Ok(new
            {
                message = req.IsActive ? "Account reactivated" : "Account deactivated",
                isActive = req.IsActive
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("users/{id:int}")]
        public IActionResult DeleteUser(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid user id." });

            var current = _authService.CurrentUser(User);
            if (current == null)
                return Unauthorized(new { message = "Not authenticated." });

            if (current.UserId == id)
                return BadRequest(new { message = "You cannot delete your own account." });

            try
            {
                _authService.DeleteUserFromDB(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "User not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (MySql.Data.MySqlClient.MySqlException ex) when (ex.Number == 1451)
            {
                return Conflict(new { message = "Cannot delete this user due to related data. Remove or reassign their data first." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Unexpected error occurred." });
            }
        }
    }
}
