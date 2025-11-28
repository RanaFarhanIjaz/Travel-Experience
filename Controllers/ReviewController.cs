using Microsoft.AspNetCore.Mvc;
using TravelShare.Models;
using TravelShare.Data;

namespace TravelShare.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReviewController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
public IActionResult Add()
{
    if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
    {
        TempData["ErrorMessage"] = "Please login to add a review.";
        return RedirectToAction("Login", "Account");
    }
    return View();
}

[HttpPost]
public async Task<IActionResult> Add(Review review, IFormFile image)
{
    if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
    {
        return RedirectToAction("Login", "Account");
    }

    if (ModelState.IsValid)
    {
        // Image upload logic remains the same
        if (image != null && image.Length > 0)
        {
            var uploads = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            review.ImagePath = $"/uploads/{fileName}";
        }

        review.Author = HttpContext.Session.GetString("UserEmail");
        _context.Reviews.Add(review);
        _context.SaveChanges();

        TempData["SuccessMessage"] = "Review added successfully!";
        return RedirectToAction("Dashboard", "Home");
    }
    return View(review);
}

        public IActionResult Details(int id)
        {
            var review = _context.Reviews.FirstOrDefault(r => r.Id == id);
            if (review == null)
                return NotFound();

            return View(review);
        }
    }
}