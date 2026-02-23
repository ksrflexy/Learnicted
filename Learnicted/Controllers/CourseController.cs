using Learnicted.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Learnicted.Controllers
{
    public class CourseController : Controller
    {
        private readonly string _groqApiKey;
        private readonly IConfiguration _configuration;

        public CourseController(IConfiguration configuration)
        {
            _configuration = configuration;
            // Anahtarı secrets.json veya appsettings içindeki AiConfig:GroqApiKey'den çekiyoruz
            _groqApiKey = _configuration["AiConfig:GroqApiKey"] ?? "YOUR_API_KEY";
        }

        public IActionResult Index()
        {
            return View(new StudyViewModel());
        }
}

        [HttpGet]
        public async Task<IActionResult> GetCourseData(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return BadRequest();

            try
            {
                using var client = new HttpClient();
                // Senin Index.cshtml'deki döngünün (v.videoId, v.title) beklediği tam format
                var prompt = $@"
                    GÖREV: '{topic}' konusu hakkında Türkçe eğitim içeriği ve 4 adet gerçek YouTube video ID'si hazırla.
                    
                    FORMAT (SADECE JSON):
                    {{
                        ""aiSummary"": ""HTML formatında özet buraya"",
                        ""videos"": [
                            {{ ""videoId"": ""vLq6_E2zXls"", ""title"": ""Video 1"", ""thumbnailUrl"": ""https://img.youtube.com/vi/vLq6_E2zXls/mqdefault.jpg"" }},
                            {{ ""videoId"": ""87uS77SCSfE"", ""title"": ""Video 2"", ""thumbnailUrl"": ""https://img.youtube.com/vi/87uS77SCSfE/mqdefault.jpg"" }},
                            {{ ""videoId"": ""ID3"", ""title"": ""Video 3"", ""thumbnailUrl"": ""https://img.youtube.com/vi/ID3/mqdefault.jpg"" }},
                            {{ ""videoId"": ""ID4"", ""title"": ""Video 4"", ""thumbnailUrl"": ""https://img.youtube.com/vi/ID4/mqdefault.jpg"" }}
                        ]
                    }}";

                var body = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[] { new { role = "user", content = prompt } },
                    temperature = 0.5,
                    response_format = new { type = "json_object" }
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey.Trim());
                var response = await client.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                var aiRawJson = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                // AI'dan gelen saf JSON string'ini bir objeye deserialize edip öyle dönüyoruz ki JS rahat okusun
                var finalData = JsonSerializer.Deserialize<object>(aiRawJson);
                return Json(finalData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}