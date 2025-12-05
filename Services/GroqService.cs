using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TravelShare.Data;
using TravelShare.Models;

namespace TravelShare.Services
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly ILogger<GroqService> _logger;
        private readonly ApplicationDbContext _context;
        
        public GroqService(HttpClient httpClient, IConfiguration configuration, 
                          ILogger<GroqService> logger, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
            _apiKey = configuration["Groq:ApiKey"];
            _model = configuration["Groq:Model"] ?? "llama3-70b-8192";
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Groq API key is missing! Check appsettings.json");
            }
            else
            {
                _logger.LogInformation($"Groq API key found. Using model: {_model}");
            }
            
            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TravelShare/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        // =================== CORE AI METHOD ===================
        public async Task<string> GetAIResponse(string prompt)
        {
            _logger.LogInformation($"GroqService.GetAIResponse called");
            
            try
            {
                var request = new
                {
                    model = _model,
                    messages = new[] 
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 500
                };
                
                _logger.LogInformation($"Sending request to Groq API with model: {_model}");
                
                var response = await _httpClient.PostAsJsonAsync(
                    "https://api.groq.com/openai/v1/chat/completions", 
                    request
                );
                
                _logger.LogInformation($"Groq API response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var firstChoice = choices[0];
                            if (firstChoice.TryGetProperty("message", out var message))
                            {
                                if (message.TryGetProperty("content", out var content))
                                {
                                    var result = content.GetString() ?? "No content";
                                    _logger.LogInformation($"Groq response success (length: {result.Length})");
                                    return result;
                                }
                            }
                        }
                        
                        _logger.LogWarning("No choices found in Groq response");
                        return "No response from AI";
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing JSON response");
                        return $"JSON Parse Error: {jsonEx.Message}";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Groq API error {response.StatusCode}: {errorContent}");
                    
                    // Try a different model if the current one fails
                    if (errorContent.Contains("model_decommissioned"))
                    {
                        return await TryAlternativeModel(prompt);
                    }
                    
                    return $"API Error ({response.StatusCode}): {errorContent}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP Request Exception");
                return $"Network Error: {httpEx.Message}";
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Request timed out");
                return "Request timed out. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAIResponse");
                return $"Error: {ex.Message}";
            }
        }
        
        // =================== ALTERNATIVE MODEL FALLBACK ===================
        private async Task<string> TryAlternativeModel(string prompt)
        {
            _logger.LogInformation("Trying alternative models...");
            
            var alternativeModels = new[]
            {
                "llama-3.3-70b-versatile",
                "llama3-70b-8192",
                "llama3-8b-8192",
                "mixtral-8x7b-32768",
                "gemma2-9b-it"
            };
            
            foreach (var model in alternativeModels)
            {
                try
                {
                    _logger.LogInformation($"Trying model: {model}");
                    
                    var request = new
                    {
                        model = model,
                        messages = new[] 
                        {
                            new { role = "user", content = prompt }
                        },
                        temperature = 0.7,
                        max_tokens = 300
                    };
                    
                    var response = await _httpClient.PostAsJsonAsync(
                        "https://api.groq.com/openai/v1/chat/completions", 
                        request
                    );
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        
                        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var firstChoice = choices[0];
                            if (firstChoice.TryGetProperty("message", out var message) &&
                                message.TryGetProperty("content", out var content))
                            {
                                _logger.LogInformation($"Success with model: {model}");
                                return content.GetString() ?? $"Response from {model}";
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            return "All models failed. Please check available models at https://console.groq.com/docs/models";
        }
        
        // =================== TRAVEL-SPECIFIC METHODS ===================
        public async Task<string> GetTravelRecommendations(string query)
        {
            var prompt = $@"You are a professional travel advisor. The user asks: '{query}'

            Provide 3-5 travel recommendations with:
            • Destination name
            • Why it's good for this query
            • Best time to visit
            • Key attractions
            
            Format as bullet points. Keep it practical and helpful.";
            
            return await GetAIResponse(prompt);
        }
        
        public async Task<string> ChatWithTravelExpert(string userMessage)
        {
            var prompt = $@"You are a friendly, knowledgeable travel expert. A traveler asks: '{userMessage}'

            Respond helpfully with:
            1. Acknowledge their question
            2. Provide specific, useful advice
            3. Be enthusiastic but accurate
            4. Use a friendly tone
            
            Keep it conversational and under 150 words.";
            
            return await GetAIResponse(prompt);
        }
        
        public async Task<string> GenerateReviewSummary(string reviewText)
        {
            var prompt = $@"Summarize this travel review in 2-3 sentences:

            {reviewText}
            
            Highlight:
            • Overall experience
            • Key positive points
            • Any criticisms
            • Would they recommend it?";
            
            return await GetAIResponse(prompt);
        }
        
        public async Task<string> FindSimilarPlaces(string placeName, string location, string description)
        {
            var prompt = $@"Suggest 3 places similar to {placeName} in {location}.

            Based on: {description}
            
            For each place, provide:
            • Name
            • Similarity to {placeName}
            • Unique feature";
            
            return await GetAIResponse(prompt);
        }
        
        public async Task<string> GetTravelTips(string destination)
        {
            var prompt = $@"Provide 5 practical travel tips for visiting {destination}:

            1. Best time to visit
            2. Cultural etiquette
            3. Transportation tips
            4. Safety advice
            5. Must-try food
            
            Make it concise and actionable.";
            
            return await GetAIResponse(prompt);
        }
        
        // =================== DATABASE ANALYSIS METHODS ===================
        public async Task<string> AnalyzeReviewTrends(string placeType = null, string location = null)
        {
            try
            {
                var query = _context.Reviews.AsQueryable();
                
                if (!string.IsNullOrEmpty(placeType))
                {
                    query = query.Where(r => r.PlaceType == placeType);
                }
                
                if (!string.IsNullOrEmpty(location))
                {
                    query = query.Where(r => r.Location.Contains(location));
                }
                
                var reviews = await query
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(20)
                    .ToListAsync();
                
                if (!reviews.Any())
                {
                    return "No reviews found in the database to analyze.";
                }
                
                var reviewSummary = string.Join("\n", reviews.Select(r => 
                    $"- {r.PlaceName} ({r.Location}): Rating {r.Rating}/5. Review: {r.Description.Substring(0, Math.Min(100, r.Description.Length))}..."));
                
                var prompt = $@"Based on these travel reviews from our database:

                {reviewSummary}

                Analyze and predict:
                1. What types of places are getting the best reviews?
                2. What common complaints or issues are mentioned?
                3. What trends are emerging in travel preferences?
                4. Predict what travelers will look for next based on these reviews
                5. Suggest improvements for places getting lower ratings

                Provide a detailed analysis with specific examples from the reviews.";
                
                return await GetAIResponse(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing review trends");
                return $"Error analyzing reviews: {ex.Message}";
            }
        }
        
        public async Task<string> GetPersonalizedRecommendations(string userEmail)
        {
            try
            {
                var userReviews = await _context.Reviews
                    .Where(r => r.Author == userEmail)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .ToListAsync();
                
                if (!userReviews.Any())
                {
                    return "We don't have enough of your past reviews to provide personalized recommendations. Please add some reviews first!";
                }
                
                var allReviews = await _context.Reviews
                    .Where(r => r.Author != userEmail)
                    .OrderByDescending(r => r.Rating)
                    .Take(50)
                    .ToListAsync();
                
                var userProfile = string.Join("\n", userReviews.Select(r => 
                    $"- You reviewed {r.PlaceName} ({r.PlaceType}) in {r.Location} and gave it {r.Rating}/5 stars"));
                
                var topReviews = string.Join("\n", allReviews.Select(r => 
                    $"- {r.PlaceName} ({r.PlaceType}) in {r.Location}: {r.Rating}/5 stars. Review: {r.Description.Substring(0, Math.Min(80, r.Description.Length))}..."));
                
                var prompt = $@"Based on this user's travel history:
                {userProfile}

                And these highly-rated reviews from other users:
                {topReviews}

                Provide personalized travel recommendations for this user:
                1. Suggest 3-5 places they would likely enjoy based on their past preferences
                2. Explain why each recommendation matches their tastes
                3. Include places similar to what they've enjoyed
                4. Suggest new types of experiences they might like to try
                5. Mention specific details from reviews that match their interests";
                
                return await GetAIResponse(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations");
                return $"Error: {ex.Message}";
            }
        }
        
        public async Task<string> CompareWithDatabase(string placeName, string location)
        {
            try
            {
                var targetPlace = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.PlaceName.Contains(placeName) || r.Location.Contains(location));
                
                if (targetPlace == null)
                {
                    var similarPlaces = await _context.Reviews
                        .Where(r => r.PlaceType == "Hotel" || r.PlaceType == "Restaurant" || r.PlaceType == "VisitingPlace")
                        .Where(r => r.Rating >= 4)
                        .Take(10)
                        .ToListAsync();
                    
                    var placeList = string.Join("\n", similarPlaces.Select(r => 
                        $"- {r.PlaceName} ({r.PlaceType}) in {r.Location}: {r.Rating}/5 stars"));
                    
                    var prompt = $@"The user is asking about {placeName} in {location}, but we don't have it in our database yet.

                    Based on these highly-rated similar places from our database:
                    {placeList}

                    Provide:
                    1. What similar, highly-rated places exist in our database
                    2. How {placeName} might compare based on typical reviews
                    3. Questions the user should ask to evaluate {placeName}
                    4. Tips for reviewing this place if they visit";
                    
                    return await GetAIResponse(prompt);
                }
                
                var similarInDb = await _context.Reviews
                    .Where(r => r.Id != targetPlace.Id)
                    .Where(r => r.PlaceType == targetPlace.PlaceType || r.Location.Contains(targetPlace.Location))
                    .Where(r => r.Rating >= 3)
                    .Take(10)
                    .ToListAsync();
                
                var comparisonData = string.Join("\n", similarInDb.Select(r => 
                    $"- {r.PlaceName} in {r.Location}: {r.Rating}/5 stars. Review: {r.Description.Substring(0, Math.Min(60, r.Description.Length))}..."));
                
                var prompt2 = $@"Comparing {targetPlace.PlaceName} ({targetPlace.PlaceType}) in {targetPlace.Location} with similar places in our database:

                Target place: {targetPlace.Rating}/5 stars. Review: {targetPlace.Description}

                Similar places in database:
                {comparisonData}

                Provide analysis:
                1. How does {targetPlace.PlaceName} compare to similar places in our database?
                2. What makes it better or worse than alternatives?
                3. Based on reviews, who would enjoy this place most?
                4. Any red flags or exceptional qualities?
                5. Final recommendation based on database comparison";
                
                return await GetAIResponse(prompt2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing with database");
                return $"Error: {ex.Message}";
            }
        }
        
        public async Task<string> PredictPlaceRating(string placeName, string placeType, string location, string description)
        {
            try
            {
                var similarPlaces = await _context.Reviews
                    .Where(r => r.PlaceType == placeType)
                    .Where(r => r.Location.Contains(location) || r.PlaceName.Contains(placeName))
                    .Take(15)
                    .ToListAsync();
                
                if (!similarPlaces.Any())
                {
                    similarPlaces = await _context.Reviews
                        .Where(r => r.PlaceType == placeType)
                        .OrderByDescending(r => r.Rating)
                        .Take(10)
                        .ToListAsync();
                }
                
                var similarData = string.Join("\n", similarPlaces.Select(r => 
                    $"- {r.PlaceName} in {r.Location}: {r.Rating}/5 stars. Review summary: {r.Description.Substring(0, Math.Min(80, r.Description.Length))}..."));
                
                var prompt = $@"Predict a rating for a new place based on similar reviews in our database:

                New Place: {placeName} ({placeType}) in {location}
                Description: {description}

                Similar places from our database with reviews:
                {similarData}

                Based on these similar reviews, predict:
                1. What rating (1-5) this new place might receive and why
                2. What aspects travelers will likely praise or criticize
                3. How it compares to existing places in the database
                4. Who would enjoy this place most
                5. Tips for getting a good experience
                
                Make it clear this is a prediction based on similar reviews, not an actual review.";
                
                return await GetAIResponse(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting rating");
                return $"Error: {ex.Message}";
            }
        }
        
        public async Task<string> GenerateTravelInsights()
        {
            try
            {
                var totalReviews = await _context.Reviews.CountAsync();
                var averageRating = await _context.Reviews.AverageAsync(r => (double)r.Rating);
                var topRated = await _context.Reviews
                    .Where(r => r.Rating == 5)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .ToListAsync();
                
                var byType = await _context.Reviews
                    .GroupBy(r => r.PlaceType)
                    .Select(g => new { Type = g.Key, Count = g.Count(), AvgRating = g.Average(r => r.Rating) })
                    .ToListAsync();
                
                var byLocation = await _context.Reviews
                    .GroupBy(r => r.Location)
                    .Select(g => new { Location = g.Key, Count = g.Count(), AvgRating = g.Average(r => r.Rating) })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();
                
                var stats = $"""
                    Database Statistics:
                    - Total Reviews: {totalReviews}
                    - Average Rating: {averageRating:F1}/5
                    
                    Ratings by Place Type:
                    {string.Join("\n", byType.Select(t => $"- {t.Type}: {t.Count} reviews, {t.AvgRating:F1}/5 average"))}
                    
                    Top Locations:
                    {string.Join("\n", byLocation.Select(l => $"- {l.Location}: {l.Count} reviews, {l.AvgRating:F1}/5 average"))}
                    
                    Recent 5-Star Reviews:
                    {string.Join("\n", topRated.Select(r => $"- {r.PlaceName} in {r.Location}: {r.Description.Substring(0, Math.Min(60, r.Description.Length))}..."))}
                    """;
                
                var prompt = $@"Based on comprehensive analysis of our travel review database:

                {stats}

                Generate deep insights:
                1. Overall trends in travel preferences
                2. What types of places consistently get high/low ratings
                3. Geographic patterns (which locations are rated highest)
                4. Common factors in 5-star vs 1-star reviews
                5. Predictions for future travel trends
                6. Recommendations for travelers based on database patterns
                7. Suggestions for places to improve based on common complaints
                
                Provide actionable insights for both travelers and business owners.";
                
                return await GetAIResponse(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating insights");
                return $"Error: {ex.Message}";
            }
        }
    }
}