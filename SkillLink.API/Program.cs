using SkillLink.API.Services;
using SkillLink.API.Services.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkillLink.API.Models;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using SkillLink.API.Seeding;
using SkillLink.API.Repositories;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Data;
using Microsoft.AspNetCore.HttpOverrides;   // <-- add

var builder = WebApplication.CreateBuilder(args);

// Repos & services (your list is fine)
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
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IReportsService, ReportsService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

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
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "SkillLink API", Version = "v1" });
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) o.IncludeXmlComments(xmlPath);

    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// CORS from env
var corsOrigins = builder.Configuration["CORS:Origins"]
               ?? Environment.GetEnvironmentVariable("CORS__Origins")
               ?? "http://localhost:3000;http://127.0.0.1:3000";
var origins = corsOrigins.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            //.SetIsOriginAllowedToAllowWildcardSubdomains() // optional
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Forwarded headers (Azure)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("JWT key not configured or too short. Set 'Jwt:Key' in App Service settings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
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

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

var shouldSeed = app.Configuration.GetValue<bool>("SeedDbOnStartup");
if (app.Environment.IsDevelopment() || shouldSeed)
{
    try { DbSeeder.Seed(app.Services); }
    catch (Exception ex) { Console.WriteLine($"[DbSeeder] Failed: {ex.Message}"); }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkillLink API v1");
    c.RoutePrefix = "swagger";
});

app.UseForwardedHeaders();      // <-- add
app.UseHttpsRedirection();      // <-- add
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
