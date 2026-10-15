using System.Text;
using System.Text.Json.Serialization;
using FencingClub.Api.Data;
using FencingClub.Api.Models;
using FencingClub.Api.Repositories;
using FencingClub.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// --- Data (Azure SQL via EF Core) ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        config.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));  // Azure SQL has transient faults; retry them.

builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

// --- Accounts (ASP.NET Core Identity) ---
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// --- Auth (JWT bearer tokens) ---
builder.Services.AddOptions<JwtOptions>()
    .Bind(config.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

// --- Push notifications (Azure Notification Hubs, or a logger when not configured) ---
builder.Services.Configure<NotificationHubOptions>(config.GetSection(NotificationHubOptions.SectionName));
if (config.GetSection(NotificationHubOptions.SectionName).Get<NotificationHubOptions>()?.IsConfigured == true)
    builder.Services.AddSingleton<IPushNotificationService, NotificationHubPushService>();
else
    builder.Services.AddSingleton<IPushNotificationService, LoggingPushService>();

// --- MVC controllers, docs, health ---
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Fencing Club API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Lets the integration tests reference the app via WebApplicationFactory<Program>.
public partial class Program;
