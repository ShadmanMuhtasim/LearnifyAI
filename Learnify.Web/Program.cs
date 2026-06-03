using Learnify.Application.Config;
using Learnify.Application.Interfaces;
using Learnify.Application.Profiles;
using Learnify.Application.Services;
using Learnify.Application.Settings;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure;
using Learnify.Infrastructure.AI;
using Learnify.Infrastructure.Data;
using Learnify.Infrastructure.Repositories;
using Learnify.Infrastructure.Services;
using Learnify.Infrastructure.UnitOfWork;
using Learnify.Web.Middleware;
using Learnify.Web.Validators;
using Learnify.Web.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using FluentValidation.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}

// Add CORS policy for frontend (Vite dev server on port 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LearnifyPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserValidator).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// Register DbContext with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IUserAiSettingsRepository, UserAiSettingsRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

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
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? string.Empty)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Background Notification Service (scoped for queueing, singleton channel for shared queue)
builder.Services.AddSingleton<INotificationService, EmailNotificationService>();

// Register Background Worker (HostedService)
builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddHttpContextAccessor();

// Register Infrastructure layer (includes AI providers, factory, and service)
builder.Services.AddInfrastructure(builder.Configuration);

// Register Background Worker for AI Processing
builder.Services.AddHostedService<AiProcessingWorker>();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        logger.LogInformation(
            "Applying EF Core migrations with ConnectionStrings:DefaultConnection ({ConnectionString}).",
            RedactConnectionString(connectionString));

        dbContext.Database.Migrate();

        logger.LogInformation("EF Core migrations applied successfully.");
    }
    catch (SqlException ex)
    {
        logger.LogError(
            ex,
            "Database startup failed for ConnectionStrings:DefaultConnection. Server='{Server}', Database='{Database}'. " +
            "Check that SQL Server is running, the server name is correct, Windows or SQL authentication matches the connection string, " +
            "and local Encrypt/TrustServerCertificate settings are valid.",
            GetConnectionStringValue(connectionString, "Data Source"),
            GetConnectionStringValue(connectionString, "Initial Catalog"));
        throw;
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(
            ex,
            "Database startup failed. Confirm ConnectionStrings:DefaultConnection exists and points to a reachable SQL Server database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("LearnifyPolicy");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Register global exception middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string RedactConnectionString(string value)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(value);
        if (!string.IsNullOrWhiteSpace(builder.Password))
        {
            builder.Password = "***";
        }

        return builder.ConnectionString;
    }
    catch
    {
        return "<invalid connection string format>";
    }
}

static string GetConnectionStringValue(string value, string key)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(value);
        return builder.ContainsKey(key) ? builder[key]?.ToString() ?? "" : "";
    }
    catch
    {
        return "";
    }
}
