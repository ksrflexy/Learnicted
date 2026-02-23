using System.Collections.Generic;

namespace Learnicted.Models
{
    public class UserProfileViewModel
    {
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Bio { get; set; }
        public string GithubUsername { get; set; }

        // --- Hataları Çözen Yeni Özellikler (CS0117 Hataları İçin) ---
        public string CompletedUnitsText { get; set; } = "0 Ünite";
        public string ActiveCoursesText { get; set; } = "0 Kurs";
        public string SuccessPointsText { get; set; } = "0 XP";
        // -----------------------------------------------------------

        public List<ProjectModel> Projects { get; set; } = new List<ProjectModel>();
        public List<string> MissingTopics { get; set; } = new List<string>();
    }

    public class ProjectModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public int Stars { get; set; }
    }
}