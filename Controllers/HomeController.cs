using Microsoft.AspNetCore.Mvc;
using TravelShare.Models;
using TravelShare.Data;
using System.Linq;

namespace TravelShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var recentReviews = _context.Reviews.OrderByDescending(r => r.CreatedDate).Take(6).ToList();
            return View(recentReviews);
        }

      public IActionResult Dashboard()
{
    if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
    {
        TempData["ErrorMessage"] = "Please login to access your dashboard.";
        return RedirectToAction("Login", "Account");
    }

    var userReviews = _context.Reviews
        .Where(r => r.Author == HttpContext.Session.GetString("UserEmail"))
        .OrderByDescending(r => r.CreatedDate)
        .ToList();

    return View(userReviews);
}       public IActionResult Search(string place, string placeType, int? minRating)
{
    var reviews = _context.Reviews.AsQueryable();

    // Search by place name, location, or description (case-insensitive)
    if (!string.IsNullOrEmpty(place))
    {
        // Convert search term to lowercase for case-insensitive search
        var searchTerm = place.ToLower();
        reviews = reviews.Where(r => 
            r.PlaceName.ToLower().Contains(searchTerm) || 
            r.Location.ToLower().Contains(searchTerm) ||
            r.Description.ToLower().Contains(searchTerm)
        );
    }

    // Filter by place type
    if (!string.IsNullOrEmpty(placeType) && placeType != "All")
    {
        reviews = reviews.Where(r => r.PlaceType == placeType);
    }

    // Filter by minimum rating
    if (minRating.HasValue && minRating > 0)
    {
        reviews = reviews.Where(r => r.Rating >= minRating.Value);
    }

    // Pass search parameters to view
    ViewBag.SearchPlace = place;
    ViewBag.SelectedType = placeType;
    ViewBag.SelectedRating = minRating;

    return View(reviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedDate).ToList());
}
    }
}