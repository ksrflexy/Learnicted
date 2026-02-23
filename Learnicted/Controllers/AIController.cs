using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Learnicted.Services;
using Learnicted.Models;
using System.Security.Claims;
using Learnicted.Data;
using Microsoft.EntityFrameworkCore;

namespace Learnicted.Controllers
{
    public class AIController : Controller
    {
        private readonly string _groqApiKey;
        private readonly YouTubeService _youtubeService;
        private readonly AppDbContext _context;

        private readonly List<string> _badWords = new List<string> { "küfür1", "küfür2", "hakaret1" };

        // Constructor'a IConfiguration ekledik ki secrets.json'ı okuyabilsin
        public AIController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _youtubeService = new YouTubeService(); // Senin orijinal halin, dokunmadım

            // Gerçek anahtarı secrets.json içindeki AiConfig:GroqApiKey'den çeker
            _groqApiKey = configuration["AiConfig:GroqApiKey"] ?? "YOUR_API_KEY";
        }

        private bool IsInappropriate(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return _badWords.Any(word => text.ToLower().Contains(word));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteHistory([FromBody] string topic)
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null) return BadRequest();

            var course = await _context.UserCourses.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CourseName == topic);
            if (course != null)
            {
                _context.UserCourses.Remove(course);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateRoadmap([FromBody] RoadmapGenerationRequest request)
        {
            if (IsInappropriate(request.UserMessage) || IsInappropriate(request.Topic))
                return Json(new { error = "Uygunsuz içerik tespit edildi." });

            try
            {
                var prompt = $@"GÖREV: '{request.Topic}' ana başlığı altında, kullanıcının şu isteğine uygun bir eğitim rotası hazırla: '{request.UserMessage}'.
                                İSTEKLER:
                                1. Tam 15 adımdan oluşmalı.
                                2. Her adım öğretici ve mantıklı bir akademik sırada olmalı.
                                3. 'Explanation' kısmında bu rotanın neden önemli olduğunu ve 'Çalışma Alanı' bölümünde neler yapılacağını açıkla.
                                JSON FORMATI: 
                                {{ 
                                    ""Explanation"": ""Rotanın genel özeti buraya"",
                                    ""Roadmap"": [""1. Adım"", ""2. Adım"", ""...15. Adım""]
                                }}";

                var aiContent = await GetGroqRawContent(prompt);
                using var doc = JsonDocument.Parse(aiContent);

                return Json(new
                {
                    Explanation = doc.RootElement.GetProperty("Explanation").GetString(),
                    Roadmap = doc.RootElement.GetProperty("Roadmap").Clone()
                });
            }
            catch (Exception ex) { return StatusCode(500, new { error = "AI Hatası: " + ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseData(string topic)
        {
            if (IsInappropriate(topic) || string.IsNullOrEmpty(topic)) return BadRequest();
            try
            {
                var prompt = $@"'{topic}' konusu için akademik düzeyde, HTML etiketleri (p, b) içeren detaylı bir Türkçe eğitim özeti hazırla. 
                                ÖNEMLİ: Yanıtına kesinlikle şu cümle ile başla: 'Konuyla ilgili videoya aşağıdan ulaşabilirsin.' 
                                Yazıların akıcı paragraflar içermeli. 
                                ÖNEMLİ 2: Özetin sonunda kullanıcıya bu konuyla ilgili bir 'Yol Haritası (Roadmap)' oluşturmak isteyip istemediğini sor.
                                Ayrıca bu konuyu en iyi anlatan videoları bulmak için kullanılacak en verimli arama terimini üret.
                                JSON FORMATI: {{ ""aiSummary"": ""özet içeriği"", ""youtubeSearchTerm"": ""en alakalı arama terimi"" }}";

                var aiContent = await GetGroqRawContent(prompt);
                using var doc = JsonDocument.Parse(aiContent);

                var summary = doc.RootElement.GetProperty("aiSummary").GetString();
                var searchTerm = doc.RootElement.TryGetProperty("youtubeSearchTerm", out var st) ? st.GetString() : topic;

                var realVideos = await _youtubeService.GetRelatedVideos(searchTerm);

                var filteredVideos = realVideos
                    .Where(v => !string.IsNullOrEmpty(v.VideoId) && v.VideoId != "dQw4w9WgXcQ")
                    .Take(4)
                    .Select(v => new { videoId = v.VideoId, title = v.Title })
                    .ToList();

                var user = await _context.Users.FirstOrDefaultAsync();
                if (user != null)
                {
                    var existingCourse = await _context.UserCourses.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CourseName == topic);
                    if (existingCourse == null)
                    {
                        _context.UserCourses.Add(new UserCourse { UserId = user.Id, CourseName = topic, IsFinished = false });
                        await _context.SaveChangesAsync();
                    }
                }

                return Json(new { aiSummary = summary, videos = filteredVideos });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserHistory()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null) return Json(new List<string>());

            var history = await _context.UserCourses
                .Where(c => c.UserId == user.Id)
                .OrderByDescending(c => c.Id)
                .Select(c => c.CourseName)
                .Take(10)
                .ToListAsync();

            return Json(history);
        }

        [HttpPost]
        public async Task<IActionResult> AskAssistant([FromBody] ChatRequest request)
        {
            if (IsInappropriate(request.Message))
                return Json(new { answer = "Uygunsuz içerik tespit edildi." });

            var messages = new List<object>();

            string systemPrompt = $"Sen bir uzman eğitmensin. Uzmanlık alanın: {request.Topic}. Sadece HTML formatında (b, p, code) cevap üret.";

            if (request.Message.ToLower().Contains("evet") || request.Message.ToLower().Contains("oluştur"))
            {
                systemPrompt += " Kullanıcı bir yol haritası istiyor. Ona adım adım ne yapması gerektiğini anlatırken 'Çalışma Alanı' bölümündeki araçları (Type Tool, Kitaplık vb.) nasıl kullanacağını tarif et.";
            }

            messages.Add(new { role = "system", content = systemPrompt });

            if (request.History != null)
            {
                foreach (var chat in request.History)
                {
                    messages.Add(new { role = chat.Role, content = chat.Content });
                }
            }

            messages.Add(new { role = "user", content = request.Message });

            try
            {
                using var client = new HttpClient();
                var body = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = messages,
                    temperature = 0.7
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey.Trim());
                var response = await client.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
                var result = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(result);

                var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return Json(new { answer = answer });
            }
            catch (Exception ex) { return Json(new { answer = "Bir hata oluştu: " + ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQuizSet([FromBody] SmartQuestionRequest request)
        {
            try
            {
                var prompt = $@"'{request.MainTopic} - {request.CurrentStepTitle}' hakkında Türkçe 5 adet çoktan seçmeli soru hazırla.
                                JSON FORMATI: {{ ""questions"": [ {{ ""question"": ""Soru"", ""options"": [""A"",""B"",""C"",""D""], ""answer"": ""Doğru Şık"", ""correctIndex"": 0 }} ] }}";

                var aiContent = await GetGroqRawContent(prompt);
                using var doc = JsonDocument.Parse(aiContent);
                var questionsList = doc.RootElement.GetProperty("questions").Clone();

                var searchResults = await _youtubeService.GetRelatedVideos($"{request.CurrentStepTitle} dersi");
                var bestVideoId = searchResults.FirstOrDefault(v => v.VideoId != "dQw4w9WgXcQ")?.VideoId ?? "vLq6_E2zXls";

                return Json(new { youtubeVideoId = bestVideoId, questions = questionsList });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeQuiz([FromBody] QuizAnalysisRequest request)
        {
            var prompt = $@"{request.Topic} konusunda {request.WrongCount} yanlışım var. Tavsiye ver.
                            JSON FORMATI: {{ ""advice"": ""tavsiye"", ""suggestedTopic"": ""alt başlık"" }}";

            var rawContent = await GetGroqRawContent(prompt);
            using var doc = JsonDocument.Parse(rawContent);

            return Json(new
            {
                advice = doc.RootElement.GetProperty("advice").GetString(),
                suggestedTopic = doc.RootElement.GetProperty("suggestedTopic").GetString()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRemediationData(string topic)
        {
            try
            {
                var prompt = $@"'{topic}' konusu hakkında telafi eğitimi içeriği hazırla. 
                                1. 'summary' alanına konuyu HTML (p, b) kullanarak detaylıca açıkla.
                                2. 'questions' dizisine bu konuyu pekiştirecek 3 adet çoktan seçmeli soru ekle.
                                JSON FORMATI: {{ 
                                    ""summary"": ""detaylı açıklama"", 
                                    ""questions"": [ 
                                        {{ ""question"": ""Soru?"", ""options"": [""A"",""B"",""C"",""D""], ""answer"": ""C"", ""correctIndex"": 2 }} 
                                    ] 
                                }}";

                var aiRawContent = await GetGroqRawContent(prompt);
                var aiDataElement = JsonDocument.Parse(aiRawContent).RootElement;

                var videoList = await _youtubeService.GetRelatedVideos($"{topic} konu anlatımı");
                var videoId = videoList.FirstOrDefault(v => v.VideoId != "dQw4w9WgXcQ")?.VideoId ?? "vLq6_E2zXls";

                return Json(new { aiData = aiDataElement.Clone(), videoId = videoId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteCourse([FromBody] string topic)
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null) return BadRequest();

            var course = await _context.UserCourses.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CourseName == topic);
            if (course != null)
            {
                course.IsFinished = true; // Konuyu tamamlandı olarak işaretle
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        private async Task<string> GetGroqRawContent(string prompt, bool isChat = false, string currentTopic = "Genel Eğitim")
        {
            using var client = new HttpClient();
            string systemMessage = isChat
                ? $"Sen bir uzman eğitmensin. Uzmanlık alanın: {currentTopic}."
                : "Sadece JSON dön. Markdown kullanma.";

            var body = new
            {
                model = "llama-3.3-70b-versatile",
                messages = new[] {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = prompt }
                },
                temperature = 0.2,
                response_format = isChat ? null : new { type = "json_object" }
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey.Trim());
            var response = await client.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", body);
            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }

    public class RoadmapGenerationRequest { public string UserMessage { get; set; } public string Topic { get; set; } }
    public class SmartQuestionRequest { public string MainTopic { get; set; } public string CurrentStepTitle { get; set; } public string Level { get; set; } }
    public class QuizAnalysisRequest { public string Topic { get; set; } public int WrongCount { get; set; } public List<string> WrongQuestions { get; set; } }

    public class ChatRequest
    {
        public string Message { get; set; }
        public string Topic { get; set; }
        public List<HistoryItem> History { get; set; }
    }
    public class HistoryItem
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}