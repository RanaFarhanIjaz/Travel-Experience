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

// Add HttpClient for AI service
builder.Services.AddHttpClient();

// Register Groq AI Service - MUST BE BEFORE builder.Build()
builder.Services.AddScoped<GroqService>();

// Configure larger file upload limits for videos and images
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

var app = builder.Build();  // LINE 190 - This is where builder becomes read-only

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
    
    // This will ensure the database is created with the latest schema
    context.Database.EnsureCreated();

    // FIX: Ensure admin user exists and has IsAdmin = true
    var adminUser = context.Users.FirstOrDefault(u => u.Email == "admin@travel.com");
    if (adminUser != null)
    {
        // Update existing admin user
        adminUser.IsAdmin = true;
        adminUser.FullName = "Admin User";
        context.SaveChanges();
        Console.WriteLine("✅ Updated existing admin user with IsAdmin = true");
    }
    else
    {
        // Create new admin user
        context.Users.Add(new User
        {
            Email = "admin@travel.com",
            Password = "admin123",
            FullName = "Admin User",
            IsAdmin = true
        });
        context.SaveChanges();
        Console.WriteLine("✅ Created new admin user with IsAdmin = true");
    }

    // Create 2 regular users
    if (!context.Users.Any(u => u.Email == "user1@travel.com"))
    {
        context.Users.Add(new User
        {
            Email = "user1@travel.com",
            Password = "user123",
            FullName = "Regular User One",
            IsAdmin = false
        });
    }
    
    if (!context.Users.Any(u => u.Email == "user2@travel.com"))
    {
        context.Users.Add(new User
        {
            Email = "user2@travel.com",
            Password = "user123",
            FullName = "Regular User Two",
            IsAdmin = false
        });
    }
    
// Test endpoint for Groq API
app.MapGet("/testgroq", async (GroqService groqService) =>
{
    try
    {
        // Simple test
        var response = await groqService.GetAIResponse("Hello, are you working? Respond with 'Yes I am working!'");
        return $"Groq Test Response: {response}";
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}\n\nStackTrace: {ex.StackTrace}";
    }
});

    context.SaveChanges();

    // Seed sample reviews if none exist
    if (!context.Reviews.Any())
    {
        context.Reviews.AddRange(
            new Review
            {
                Title = "Amazing Beach Resort",
                Description = "Beautiful location with excellent service and amenities. The private beach was stunning and the food was exceptional.",
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
                Description = "Authentic Italian cuisine with great atmosphere. The pasta was homemade and the service was outstanding.",
                PlaceName = "Mario's Trattoria",
                PlaceType = "Restaurant",
                Rating = 4,
                Location = "New York, NY",
                Author = "admin@travel.com",
                CreatedDate = DateTime.Now.AddDays(-3)
            },
            new Review
            {
                Title = "Beautiful Mountain View",
                Description = "Breathtaking views and peaceful environment. Perfect for hiking and nature lovers.",
                PlaceName = "Mountain Peak Park",
                PlaceType = "VisitingPlace",
                Rating = 5,
                Location = "Colorado Springs, CO",
                Author = "user1@travel.com",
                CreatedDate = DateTime.Now.AddDays(-1)
            },
            new Review
            {
                Title = "Luxury City Hotel",
                Description = "Modern amenities with excellent city views. The rooftop bar was amazing.",
                PlaceName = "Grand City Hotel",
                PlaceType = "Hotel",
                Rating = 4,
                Location = "Chicago, Illinois",
                Author = "admin@travel.com",
                CreatedDate = DateTime.Now.AddDays(-2)
            },
            new Review
            {
                Title = "Cozy Family Restaurant",
                Description = "Great food and family-friendly atmosphere. Perfect for weekend dinners.",
                PlaceName = "Family Diner",
                PlaceType = "Restaurant",
                Rating = 4,
                Location = "Austin, Texas",
                Author = "user2@travel.com",
                CreatedDate = DateTime.Now.AddDays(-4)
            },
            new Review
            {
                Title = "Historical Museum Tour",
                Description = "Fascinating exhibits and knowledgeable guides. Great educational experience.",
                PlaceName = "History Museum",
                PlaceType = "VisitingPlace",
                Rating = 5,
                Location = "Washington, DC",
                Author = "user1@travel.com",
                CreatedDate = DateTime.Now.AddDays(-6)
            }
        );
        context.SaveChanges();
    }
}

app.Run();