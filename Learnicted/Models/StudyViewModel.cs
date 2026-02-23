using Learnicted.Services; // VideoModel'i görmesi için gerekebilir

namespace Learnicted.Models
{
    public class StudyViewModel
    {
        public string Subject { get; set; }
        public string AiSummary { get; set; }

        public List<VideoModel> Videos { get; set; } // YouTubeVideo yerine VideoModel yaptık
    }
}