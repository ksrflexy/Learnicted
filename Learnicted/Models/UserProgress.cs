using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Learnicted.Models;

public class UserProgress
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("User")]
    public int UserId { get; set; }

    public string SelectedCourse { get; set; } = string.Empty;
    public string WeakPointsJson { get; set; } = "[]";
    public int TotalXP { get; set; } = 0;
    public int DailyStreak { get; set; } = 1;

    public User User { get; set; } = null!;
}
