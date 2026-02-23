using System.Collections.Generic;

namespace Learnicted.Models // Proje adın farklıysa burayı kendi namespace'inle güncelle
{
    public class AIRequest
    {
        public string UserMessage { get; set; }
        public string Topic { get; set; }
    }

    public class AIQuestionResponse
    {
        public string Explanation { get; set; } // Konu anlatımı
        public string Question { get; set; }    // Soru metni
        public List<string> Options { get; set; } // Şıklar
        public string CorrectAnswer { get; set; } // Doğru cevap
    }
}