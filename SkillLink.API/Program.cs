using SkillLink.API.Services;
using SkillLink.API.Services.Abstractions; // <-- interfaces for API services
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkillLink.API.Models;
using System.Security.Claims;
using SkillLink.API.Services;
using SkillLink.API.Services.Abstractions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using SkillLink.API.Seeding;
using SkillLink.API.Repositories;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Data;



// ----------------------------------------
// CONFIGURE SERVICES
// ----------------------------------------
var builder = WebApplication.CreateBuilder(args);

// Services & Repos
builder.Services.AddSingleton<DbHelper>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAcceptedRequestRepository, AcceptedRequestRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<ITutorPostRepository, TutorPostRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IFeedRepository, FeedRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<ITutorPostService, TutorPostService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IAcceptedRequestService, AcceptedRequestService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IFeedService, FeedService>();

builder.Services.AddControllers();

// ----- Swagger/OpenAPI -----
builder.Services.AddEndpointsApiExplorer(); // keep only once
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillLink API", Version = "v1" });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ----- CORS (single policy driven by config/env) -----
var corsOrigins = builder.Configuration["CORS:Origins"]
               ?? Environment.GetEnvironmentVariable("CORS__Origins")
               ?? "http://localhost:3000;http://127.0.0.1:3000";

var origins = corsOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
              // .AllowCredentials(); // Only if you are using cookies/sessions
    });
});

// ----- JWT -----
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT key not configured or too short. Set 'Jwt:Key' in App Service settings.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

// ----------------------------------------
// BUILD PIPELINE
// ----------------------------------------
var app = builder.Build();

// Optional: Redirect root to Swagger (avoids 500 at "/")
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Run seeder only in Development or when a flag is set
var shouldSeed = app.Configuration.GetValue<bool>("SeedDbOnStartup");
if (app.Environment.IsDevelopment() || shouldSeed)
{
    try
    {
        DbSeeder.Seed(app.Services);
        Console.WriteLine("[DbSeeder] Seeded test users (admin/learner/tutor).");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DbSeeder] Failed seeding: {ex.Message}");
    }
}

// Swagger in Dev; optional in Prod
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillLink API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
