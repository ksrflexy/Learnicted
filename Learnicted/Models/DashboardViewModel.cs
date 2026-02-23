namespace Learnicted.Models;

public class DashboardViewModel
{
    public User UserInfo { get; set; } = null!;
    public UserProgress? Progress { get; set; }
}