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
        public async Task<IActionResult> Add(Review review, IFormFile image, IFormFile video)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Image upload
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

                // Video upload
                if (video != null && video.Length > 0)
                {
                    // Validate video file type
                    var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".webm" };
                    var fileExtension = Path.GetExtension(video.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("video", "Only video files are allowed (MP4, AVI, MOV, WMV, WEBM).");
                        return View(review);
                    }

                    // Validate file size (50MB limit)
                    if (video.Length > 50 * 1024 * 1024)
                    {
                        ModelState.AddModelError("video", "Video file size must be less than 50MB.");
                        return View(review);
                    }

                    var uploads = Path.Combine(_environment.WebRootPath, "uploads", "videos");
                    if (!Directory.Exists(uploads))
                        Directory.CreateDirectory(uploads);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(video.FileName);
                    var filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await video.CopyToAsync(stream);
                    }

                    review.VideoPath = $"/uploads/videos/{fileName}";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                TempData["ErrorMessage"] = "Please login to delete reviews.";
                return RedirectToAction("Login", "Account");
            }

            var review = _context.Reviews.FirstOrDefault(r => r.Id == id);
            if (review == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "true";

            // Check if user is admin OR the review owner
            if (!isAdmin && userEmail != review.Author)
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this review.";
                return RedirectToAction("Dashboard", "Home");
            }

            // Delete associated files
            if (!string.IsNullOrEmpty(review.ImagePath))
            {
                var imagePath = Path.Combine(_environment.WebRootPath, review.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            if (!string.IsNullOrEmpty(review.VideoPath))
            {
                var videoPath = Path.Combine(_environment.WebRootPath, review.VideoPath.TrimStart('/'));
                if (System.IO.File.Exists(videoPath))
                {
                    System.IO.File.Delete(videoPath);
                }
            }

            _context.Reviews.Remove(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Review deleted successfully!";
            return RedirectToAction("Dashboard", "Home");
        }
    }
}