using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cosmetics.Service.Gemini
{
    public class GeminiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApi:ApiKey"];
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
        }

        public async Task<string> GetChatResponse(string userMessage)
        {
            var requestData = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userMessage }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var url = $"v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine("Quota exceeded. Waiting before retry...");
                    await Task.Delay(30000); // Đợi 30 giây
                    response = await _httpClient.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        errorContent = await response.Content.ReadAsStringAsync();
                        return $"Error: {response.StatusCode} - {errorContent}";
                    }
                }
                else
                {
                    return $"Error: {response.StatusCode} - {errorContent}";
                }
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw Response: " + responseContent);

            var jsonResponse = JsonSerializer.Deserialize<JsonResponse>(responseContent);

            if (jsonResponse?.Error != null)
            {
                return $"API Error: {jsonResponse.Error.Message}";
            }

            if (jsonResponse?.Candidates == null || jsonResponse.Candidates.Length == 0)
            {
                return "No candidates found in response. Raw response: " + responseContent;
            }

            return jsonResponse.Candidates[0]?.Content?.Parts[0]?.Text ?? "No response";
        }
    }

    internal class JsonResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; }

        [JsonPropertyName("error")]
        public GeminiError? Error { get; set; }

        public class Candidate
        {
            [JsonPropertyName("content")]
            public Content Content { get; set; }
        }

        public class Content
        {
            [JsonPropertyName("parts")]
            public Part[] Parts { get; set; }

            [JsonPropertyName("role")]
            public string Role { get; set; }
        }

        public class Part
        {
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public class GeminiError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("status")]
            public string Status { get; set; }
        }
    }
}
