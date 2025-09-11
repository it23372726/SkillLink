using Microsoft.AspNetCore.Mvc;
using SkillLink.API.Models;
using SkillLink.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace SkillLink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
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
            {
                return Unauthorized(new { message = "Invalid token or user not logged in" });
            }
            
            var profile = _authService.GetUserProfile(user.UserId);
            return Ok(profile);
        }
        [Authorize]
        [HttpPut("profile")]
        public IActionResult UpdateUserProfile(UpdateProfileRequest request)
        {
            var user = _authService.CurrentUser(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token or user not logged in" });
            }
            
            var success = _authService.UpdateUserProfile(user.UserId, request);
            if (!success)
            {
                return BadRequest(new { message = "Failed to update profile" });
            }
            
            return Ok(new { message = "Profile updated successfully" });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {   
            Console.WriteLine($"Fetched User controller me-> {User}");
            var user = _authService.CurrentUser(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid token or user not logged in" });
            }

            return Ok(user);
        }

        [HttpGet("by-userId/{id}")]
        public IActionResult GetUserById(int id){
            var req = _authService.GetUserById(id);
            if (req == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(req);
        }


        // Register User
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterRequest req)
        {
            try
            {
                // Server-side required validations
                if (string.IsNullOrWhiteSpace(req.FullName) ||
                    string.IsNullOrWhiteSpace(req.Email) ||
                    string.IsNullOrWhiteSpace(req.Password))
                {
                    return BadRequest(new { message = "Full name, email, and password are required." });
                }

                // Optional: handle profile picture upload
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

                    profilePicPath = "/uploads/" + fileName; // relative URL
                }

                // Pass through to service (this enforces: no dup email, not disposable, sends verification email)
                _authService.Register(new RegisterRequest
                {
                    FullName = req.FullName,
                    Email = req.Email,
                    Password = req.Password,
                    Role = string.IsNullOrWhiteSpace(req.Role) ? "Learner" : req.Role,
                    ProfilePicturePath = profilePicPath
                });

                return Ok(new
                {
                    message = "User registered successfully. Please check your email to verify your account."
                });
            }
            catch (ArgumentException ex)
            {
                // Triggered by missing FullName/Email/Password in the service (backup)
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Triggered by duplicate email OR disposable domain rejection
                // We'll map both to conflict (409)
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

        public class UpdateTeachModeRequest { public bool ReadyToTeach { get; set; } }

        [Authorize]
        [HttpPut("teach-mode")]
        public IActionResult UpdateTeachMode(UpdateTeachModeRequest req)
        {
            var me = _authService.CurrentUser(User);
            if (me == null) return Unauthorized();

            var ok = _authService.UpdateTeachMode(me.UserId, req.ReadyToTeach);
            if (!ok) return BadRequest(new { message = "Failed to update mode" });

            // return fresh profile snippet (optional)
            var profile = _authService.GetUserProfile(me.UserId);
            return Ok(new { message = "Updated", readyToTeach = profile.ReadyToTeach });
        }

        public class UpdateActiveRequest { public bool IsActive { get; set; } }

         [Authorize]
        [HttpPut("active")]
        public IActionResult UpdateActive([FromBody] UpdateActiveRequest req)
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

         [Authorize]
         [HttpDelete("delete")]
         public IActionResult DeleteUser(int id){
            try
            {
                _authService.DeleteUserFromDB(id);
                return Ok( new {message = "User deleted!"});
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

         }

    }
}

