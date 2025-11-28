using System.ComponentModel.DataAnnotations;

namespace TravelShare.Models
{
    public class Review
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string PlaceName { get; set; } = string.Empty;
        
        [Required]
        public string PlaceType { get; set; } = string.Empty; // Hotel, Restaurant, VisitingPlace
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        public string Location { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string Author { get; set; } = string.Empty;
    }
}