using Microsoft.EntityFrameworkCore;
using TravelShare.Data;
using TravelShare.Models;
using TravelShare.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure SQLite Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=travelshare.db"));

// Register services
builder.Services.AddScoped<RecommendationService>();

// Add Antiforgery service
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    
    // Seed some initial data
    if (!context.Users.Any())
    {
        context.Users.Add(new User 
        { 
            Email = "admin@travel.com", 
            Password = "admin123", 
            FullName = "Admin User" 
        });
        
        // Add some sample reviews
        if (!context.Reviews.Any())
        {
            context.Reviews.AddRange(
                new Review
                {
                    Title = "Amazing Beach Resort",
                    Description = "Beautiful location with excellent service and amenities.",
                    PlaceName = "Sunset Beach Resort",
                    PlaceType = "Hotel",
                    Rating = 5,
                    Location = "Miami, Florida",
                    Author = "admin@travel.com",
                    CreatedDate = DateTime.Now.AddDays(-5)
                },
                new Review
                {
                    Title = "Best Italian Food",
                    Description = "Authentic Italian cuisine with great atmosphere.",
                    PlaceName = "Mario's Trattoria",
                    PlaceType = "Restaurant",
                    Rating = 4,
                    Location = "New York, NY",
                    Author = "admin@travel.com",
                    CreatedDate = DateTime.Now.AddDays(-3)
                }
            );
        }
        context.SaveChanges();
    }
}

app.Run();