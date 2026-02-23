using Newtonsoft.Json;
using System.Text;

namespace Learnicted.Services
{
    public class AiService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AiService(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AiConfig:GroqApiKey"]}");
        }

        public async Task<string> AskQuestion(string prompt, bool isPlus, int currentDailyCount)
        {
            var requestBody = new
            {
                model = _config["AiConfig:ModelName"],
                messages = new[] {
                    new { role = "system", content = "Sen bir eğitim asistanısın. Konuları kısa ve öz açıkla." },
                    new { role = "user", content = prompt }
                }
            };

            var response = await _httpClient.PostAsync("https://api.groq.com/openai/v1/chat/completions",
                new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                dynamic json = JsonConvert.DeserializeObject(result);
                return json.choices[0].message.content;
            }
            return "AI şu an yanıt veremiyor.";
        }
    }
}