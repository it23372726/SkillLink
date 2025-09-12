using SkillLink.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<DbHelper>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<AcceptedRequestService>();
builder.Services.AddScoped<AdminService>();
//builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<EmailService>();





builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // OpenAPI docs

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        ),
        RoleClaimType = ClaimTypes.Role
    };
});

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // frontend URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


builder.Services.AddSignalR();




builder.Services.AddAuthorization();

var app = builder.Build();

// Middleware order
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Swagger/OpenAPI UI
}
app.UseCors(MyAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseStaticFiles(); // enables serving wwwroot files


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
