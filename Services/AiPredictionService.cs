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

        // Calls the "data": [ ... ] style API (dense_v5_API.py)
        public async Task<string> GetRawPredictionAsync(object features)
        {
            var endpoint = _configuration["AiSettings:Endpoint"] ?? "http://127.0.0.1:8000/predict";
            var apiKey = _configuration["AiSettings:ApiKey"] ?? "";

            var payload = new { data = new[] { features } };

            return await SendRequestAsync(endpoint, apiKey, payload);
        }

        // Calls the "features": { ... } style API (two_head_model API)
        public async Task<string> GetFeaturePredictionAsync(object features)
        {
            var endpoint = _configuration["AiSettings:Endpoint"] ?? "http://127.0.0.1:8000/predict";
            var apiKey = _configuration["AiSettings:ApiKey"] ?? "";

            var payload = new { features = features };

            return await SendRequestAsync(endpoint, apiKey, payload);
        }

        private async Task<string> SendRequestAsync(string endpoint, string apiKey, object payload)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Serialize(new
                    {
                        error = $"API Error: {response.StatusCode}",
                        details = errorDetails
                    });
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = $"Connection failed: {ex.Message}" });
            }
        }
    }
}