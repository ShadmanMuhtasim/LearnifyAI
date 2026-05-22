using Learnify.Application.Profiles;
using Learnify.Core.Interfaces;
using Learnify.Infrastructure.Data;
using Learnify.Infrastructure.Repositories;
using Learnify.Infrastructure.UnitOfWork;
using Learnify.Web.Middleware;
using Learnify.Web.Validators;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateUserValidator).Assembly);

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
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
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

app.UseRouting();

app.UseAuthorization();

// Register global exception middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();