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
using DotNetEnv;

// ----------------------------------------
// CONFIGURE SERVICES
// ----------------------------------------
var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    Env.Load();
}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables(); // load env vars from Azure or GitHub secrets

// Add services
builder.Services.AddSingleton<DbHelper>();
// builder.Services.AddScoped<RequestService>();
// builder.Services.AddScoped<SessionService>();

// Register interfaces -> implementations (make sure classes implement these)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<EmailService>();

// builder.Services.AddScoped<SkillService>();
// builder.Services.AddScoped<AcceptedRequestService>();
// builder.Services.AddScoped<TutorPostService>();
// builder.Services.AddScoped<FriendshipService>();
// builder.Services.AddScoped<ReactionService>();
// builder.Services.AddScoped<CommentService>();
// builder.Services.AddScoped<FeedService>();

// If your controllers depend on interfaces, register them like this:
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

// For Minimal API OpenAPI (optional new .NET feature)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // OpenAPI docs


var corsOrigins = builder.Configuration["CORS:Origins"]  // e.g., "http://localhost:3000;http://127.0.0.1:3000"
                  ?? Environment.GetEnvironmentVariable("CORS__Origins")
                  ?? "http://localhost:3000;http://127.0.0.1:3000";

var origins = corsOrigins
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(origins)                  // must be explicit if using credentials
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()                   // only if you're using cookies/session
            .WithExposedHeaders("Authorization", "X-Session-Id", "X-Total-Count"); // expose custom headers if needed
    });

    options.AddPolicy("AllowAllDev", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// Required for Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Basic metadata
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SkillLink API",
        Version = "v1",
        Description = "Backend API for SkillLink platform",
        Contact = new OpenApiContact { Name = "SkillLink Team", Email = "support@skilllink.local" }
    });

    // XML comments (if enabled in csproj)
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // 🔐 JWT Bearer auth support in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

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
            Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "dev-fallback-key-please-set-in-appsettings")
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

// ----------------------------------------
// BUILD PIPELINE
// ----------------------------------------
var app = builder.Build();

try
{
    DbSeeder.Seed(app.Services);
    Console.WriteLine("[DbSeeder] Seeded test users (admin/learner/tutor).");
}
catch (Exception ex)
{
    Console.WriteLine($"[DbSeeder] Failed seeding: {ex.Message}");
}

// 👉 Option A: Enable Swagger in all environments (dev/test/prod)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillLink API v1");
    c.RoutePrefix = "swagger"; // URL: /swagger
    
    c.DocExpansion(DocExpansion.Full);  // expand all sections
    c.DefaultModelsExpandDepth(-1);     // hide "Schemas"
});

// If you also want the new minimal OpenAPI explorer only in Dev:
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Swagger/OpenAPI UI (new .NET 9 feature)
}

app.UseCors(MyAllowSpecificOrigins);

// NOTE: In Docker, if you're only exposing HTTP (no 443), HTTPS redirect can cause a warning.
// Keep it if you want local HTTPS; otherwise you can comment it when running only over HTTP.
// app.UseHttpsRedirection();

app.UseStaticFiles(); // enables serving wwwroot files

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// If you have hubs later:
// app.MapHub<YourHub>("/hubs/notifications");

app.Run();
