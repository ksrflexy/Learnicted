using System.ComponentModel.DataAnnotations;

namespace Learnicted.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsPlus { get; set; } = false;
        public int Streak { get; set; } = 0;

        public string? Bio { get; set; }
        public string? GithubUsername { get; set; }

        public UserProgress? Progress { get; set; }
    }
}
