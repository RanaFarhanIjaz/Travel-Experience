using System.ComponentModel.DataAnnotations;

namespace TravelShare.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public string FullName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; } = DateTime.Now;
    }
}