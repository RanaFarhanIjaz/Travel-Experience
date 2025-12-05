using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            // Show featured reviews on home page
            var featuredReviews = _context.Reviews
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedDate)
                .Take(6)
                .ToList();
            
            return View(featuredReviews);
        }

        public IActionResult Dashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                TempData["ErrorMessage"] = "Please login to view your dashboard.";
                return RedirectToAction("Login", "Account");
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            List<Review> reviews;

            if (isAdmin)
            {
                // Admin sees ALL reviews
                reviews = _context.Reviews
                    .OrderByDescending(r => r.CreatedDate)
                    .ToList();
            }
            else
            {
                // Regular users see only their own reviews
                reviews = _context.Reviews
                    .Where(r => r.Author == userEmail)
                    .OrderByDescending(r => r.CreatedDate)
                    .ToList();
            }

            return View(reviews);
        }

        public IActionResult Search(string search, string placeType, int minRating = 0)
        {
            IQueryable<Review> query = _context.Reviews;

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => 
                    r.PlaceName.Contains(search) || 
                    r.Location.Contains(search) || 
                    r.Description.Contains(search) ||
                    r.Title.Contains(search));
            }

            // Apply place type filter
            if (!string.IsNullOrEmpty(placeType) && placeType != "All Types")
            {
                query = query.Where(r => r.PlaceType == placeType);
            }

            // Apply rating filter
            if (minRating > 0)
            {
                query = query.Where(r => r.Rating >= minRating);
            }

            var reviews = query
                .OrderByDescending(r => r.CreatedDate)
                .ToList();

            return View(reviews);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}