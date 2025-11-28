using TravelShare.Models;
using TravelShare.Data;
using System.Linq;

namespace TravelShare.Services
{
    public class RecommendationService
    {
        private readonly ApplicationDbContext _context;

        public RecommendationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Review> GetRecommendedReviews(string userEmail)
        {
            var userReviews = _context.Reviews
                .Where(r => r.Author == userEmail)
                .ToList();

            if (!userReviews.Any())
            {
                // If no user reviews, return highest rated reviews
                return _context.Reviews
                    .OrderByDescending(r => r.Rating)
                    .ThenByDescending(r => r.CreatedDate)
                    .Take(6)
                    .ToList();
            }

            // Simple recommendation based on user's preferred place types
            var preferredTypes = userReviews
                .GroupBy(r => r.PlaceType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(2)
                .ToList();

            var recommendations = _context.Reviews
                .Where(r => r.Author != userEmail && preferredTypes.Contains(r.PlaceType))
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedDate)
                .Take(6)
                .ToList();

            return recommendations;
        }

        public List<Review> GetSimilarReviews(int reviewId)
        {
            var currentReview = _context.Reviews.FirstOrDefault(r => r.Id == reviewId);
            if (currentReview == null)
                return new List<Review>();

            return _context.Reviews
                .Where(r => r.Id != reviewId && 
                           (r.PlaceType == currentReview.PlaceType || 
                            r.Location.Contains(currentReview.Location.Split(',')[0])))
                .OrderByDescending(r => r.Rating)
                .Take(4)
                .ToList();
        }
    }
}