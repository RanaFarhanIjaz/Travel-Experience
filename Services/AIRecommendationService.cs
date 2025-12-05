using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using TravelShare.Models;
using TravelShare.Data;

namespace TravelShare.Services
{
    public class AIRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AIRecommendationService(ApplicationDbContext context, HttpClient httpClient, string apiKey)
        {
            _context = context;
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<List<Review>> GetAIRecommendationsAsync(string userPreferences, int count = 6)
{
    try
    {
        var allReviews = _context.Reviews.ToList();
        
        if (!allReviews.Any())
            return new List<Review>(); // Return empty instead of popular

        // Get AI suggestions
        var aiSuggestions = await GetAISuggestionsAsync(userPreferences, allReviews);
        
        // Filter and validate AI suggestions against our database
        var validReviews = FilterValidReviews(aiSuggestions, allReviews, count);
        
        // ONLY return AI-suggested reviews, never fall back to all reviews
        return validReviews;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AI Recommendation Error: {ex.Message}");
        return new List<Review>(); // Return empty instead of popular
    }
}

        private async Task<List<string>> GetAISuggestionsAsync(string userPreferences, List<Review> reviews)
        {
            // Get only the real place names from database
            var realPlaceNames = reviews.Select(r => r.PlaceName).Distinct().ToList();
            
            var prompt = $"""
            I have a travel website with ONLY these exact places available:
            {string.Join(", ", realPlaceNames)}

            User is looking for: "{userPreferences}"

            CRITICAL INSTRUCTIONS:
            1. You MUST ONLY suggest places from the exact list above
            2. Do NOT invent, create, or suggest any new places
            3. Do NOT include any explanations or additional text
            4. Return exactly 6 place names as a comma-separated list
            5. If you can't find perfect matches, choose the closest ones from the list

            Required format: "Place1, Place2, Place3, Place4, Place5, Place6"

            SUGGESTED PLACES:
            """;

            var requestData = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                model = "llama-3.1-8b-instant",
                max_tokens = 150,
                temperature = 0.3, // Lower temperature for more consistent results
                stream = false
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonSerializer.Deserialize<GroqResponse>(responseContent);

            return ParseAISuggestions(groqResponse.choices[0].message.content, realPlaceNames);
        }

       private List<string> ParseAISuggestions(string aiResponse, List<string> validPlaceNames)
{
    var suggestions = new List<string>();
    
    if (string.IsNullOrEmpty(aiResponse))
    {
        Console.WriteLine("AI response is empty");
        return new List<string>(); // Return empty instead of fallback
    }

    Console.WriteLine($"Raw AI Response: {aiResponse}");

    // Clean and extract the comma-separated list
    var cleanResponse = aiResponse.Trim()
        .Replace("\"", "")
        .Replace("'", "")
        .Replace("Suggested places:", "")
        .Replace("SUGGESTED PLACES:", "")
        .Replace("Places:", "")
        .Split('\n')[0] // Take only first line
        .Trim();

    // Split by comma and validate each place
    var parts = cleanResponse.Split(',', StringSplitOptions.RemoveEmptyEntries);
    
    foreach (var part in parts)
    {
        var placeName = part.Trim();
        
        // Remove any numbering or bullets
        placeName = System.Text.RegularExpressions.Regex.Replace(placeName, @"^\d+\.\s*", "");
        placeName = System.Text.RegularExpressions.Regex.Replace(placeName, @"^-\s*", "");
        placeName = System.Text.RegularExpressions.Regex.Replace(placeName, @"^\*\s*", "");
        placeName = placeName.TrimEnd('.', ' ');
        
        // STRICT VALIDATION: Only add if exact match exists in database
        var exactMatch = validPlaceNames.FirstOrDefault(p => 
            p.Equals(placeName, StringComparison.OrdinalIgnoreCase));
        
        if (!string.IsNullOrEmpty(exactMatch) && !suggestions.Contains(exactMatch))
        {
            suggestions.Add(exactMatch);
            Console.WriteLine($"✅ Valid AI suggestion: {exactMatch}");
        }
        else if (!string.IsNullOrEmpty(placeName))
        {
            Console.WriteLine($"❌ Invalid AI suggestion skipped: {placeName}");
        }
    }

    Console.WriteLine($"Final valid suggestions count: {suggestions.Count}");
    return suggestions;
}
private List<Review> FilterValidReviews(List<string> aiSuggestions, List<Review> allReviews, int count)
{
    var validReviews = new List<Review>();
    
    Console.WriteLine($"AI Valid Suggestions: {string.Join(", ", aiSuggestions)}");

    // STRICT MATCHING: Only add reviews that exactly match AI suggestions
    foreach (var placeName in aiSuggestions)
    {
        var review = allReviews.FirstOrDefault(r => 
            r.PlaceName.Equals(placeName, StringComparison.OrdinalIgnoreCase));
        
        if (review != null && !validReviews.Contains(review))
        {
            validReviews.Add(review);
            Console.WriteLine($"✅ Added valid review: {review.PlaceName}");
        }
    }

    // If AI returned NO valid suggestions, return empty list instead of all reviews
    if (validReviews.Count == 0)
    {
        Console.WriteLine("❌ No valid AI suggestions found, returning empty");
        return new List<Review>();
    }

    // If we have SOME valid suggestions but not enough, only return what we have
    if (validReviews.Count < count)
    {
        Console.WriteLine($"⚠️ Only found {validReviews.Count} valid suggestions");
        // Return only the valid ones we found, don't add random ones
        return validReviews;
    }

    Console.WriteLine($"✅ Final valid reviews: {validReviews.Count}");
    return validReviews.Take(count).ToList();
}

        private List<string> GetFallbackPlaces(List<string> validPlaceNames, int count = 6)
        {
            // Return random places from database as fallback
            var random = new Random();
            return validPlaceNames
                .OrderBy(x => random.Next())
                .Take(count)
                .ToList();
        }

        public List<Review> GetPopularReviews(int count)
        {
            return _context.Reviews
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.CreatedDate)
                .Take(count)
                .ToList();
        }
    }

    public class GroqResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}