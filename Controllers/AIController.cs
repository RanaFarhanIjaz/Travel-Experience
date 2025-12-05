using Microsoft.AspNetCore.Mvc;
using TravelShare.Services;
using TravelShare.Data;
using Microsoft.EntityFrameworkCore;

namespace TravelShare.Controllers
{
    public class AIController : Controller
    {
        private readonly GroqService _groqService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIController> _logger;
        
        public AIController(GroqService groqService, ApplicationDbContext context, ILogger<AIController> logger)
        {
            _groqService = groqService;
            _context = context;
            _logger = logger;
        }
        
        // GET: /AI/Index (Test page)
        public IActionResult Index()
        {
            return View();
        }
        
        // POST: /AI/Recommendations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recommendations(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Please enter a travel query" });
                }
                
                _logger.LogInformation($"AI Request: {query}");
                var recommendations = await _groqService.GetTravelRecommendations(query);
                
                return Json(new { 
                    success = true, 
                    recommendations = recommendations 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Recommendations error");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
        
        // POST: /AI/SummarizeReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummarizeReview(int id)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found" });
                }
                
                var summary = await _groqService.GenerateReviewSummary(review.Description);
                
                return Json(new { 
                    success = true, 
                    summary = summary,
                    reviewId = id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Summarize error");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
        
        // POST: /AI/SimilarPlaces
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimilarPlaces(int id)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(id);
                if (review == null)
                {
                    return Json(new { success = false, message = "Review not found" });
                }
                
                var similar = await _groqService.FindSimilarPlaces(
                    review.PlaceName, 
                    review.Location, 
                    review.Description
                );
                
                return Json(new { 
                    success = true, 
                    similar = similar,
                    placeName = review.PlaceName 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Similar Places error");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
        
        // POST: /AI/TravelTips
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TravelTips(string destination)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destination))
                {
                    return Json(new { success = false, message = "Please enter a destination" });
                }
                
                var tips = await _groqService.GetTravelTips(destination);
                
                return Json(new { 
                    success = true, 
                    tips = tips,
                    destination = destination 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Travel Tips error");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
        
        // POST: /AI/Chat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    return Json(new { success = false, message = "Please enter a message" });
                }
                
                var response = await _groqService.ChatWithTravelExpert(message);
                
                return Json(new { 
                    success = true, 
                    response = response 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Chat error");
                return Json(new { 
                    success = false, 
                    message = $"Error: {ex.Message}" 
                });
            }
        }
        
        // GET: /AI/Test (Simple test endpoint)
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            try
            {
                var testResponse = await _groqService.GetAIResponse("Hello, are you working? Respond in one sentence.");
                return Content($"Groq AI Test: {testResponse}", "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"Test failed: {ex.Message}", "text/plain");
            }
        }
    }
}