using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IPS_PROJECT.Services
{
    public class AiPredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiPredictionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetRawPredictionAsync(object features)
        {
           
            var endpoint = _configuration["AiSettings:Endpoint"] ?? "http://127.0.0.1:8000/predict";
            var apiKey = _configuration["AiSettings:ApiKey"] ?? "";

            try
            {
                
                var payload = new { features = features };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return $"{{\"error\": \"API Error: {response.StatusCode}\", \"details\": \"{errorDetails.Replace("\"", "'")}\"}}";
                }

                
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
             
                return $"{{\"error\": \"Connection failed: {ex.Message}\"}}";
            }
        }
    }
}