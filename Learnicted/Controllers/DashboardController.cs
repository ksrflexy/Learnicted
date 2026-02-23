using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learnicted.Data;
using Learnicted.Models;
using System.Text.Json;
using System.Security.Claims;

namespace Learnicted.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    // Giriş yapmış kullanıcıyı bulur
    private async Task<User?> GetCurrentUserAsync()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim)) return null;

        int userId = int.Parse(userIdClaim);
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }

    private async Task SetupSidebarData(User user)
    {
        var progress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == user.Id);
        ViewBag.FullName = user.FullName;
        ViewBag.UserName = user.UserName;
        ViewBag.UserId = user.Id;
        ViewBag.Streak = progress?.DailyStreak ?? 0;
        ViewBag.IsPlus = true;
    }

    // Index metoduna selectedTopic parametresini ekledik (Sihirbazdan gelen veri için)
    public async Task<IActionResult> Index(string? selectedTopic)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        await SetupSidebarData(user);
        var progress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == user.Id);

        // --- Yeni Mantık: Sihirbazdan bir konu seçildiyse kaydet ---
        if (!string.IsNullOrEmpty(selectedTopic))
        {
            if (progress == null)
            {
                progress = new UserProgress
                {
                    UserId = user.Id,
                    SelectedCourse = selectedTopic,
                    TotalXP = 0,
                    DailyStreak = 1
                };
                _context.UserProgresses.Add(progress);
            }
            else
            {
                progress.SelectedCourse = selectedTopic;
                _context.UserProgresses.Update(progress);
            }
            await _context.SaveChangesAsync();
        }
        // ---------------------------------------------------------

        return View(new DashboardViewModel { UserInfo = user, Progress = progress });
    }

    public async Task<IActionResult> Roadmap()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        await SetupSidebarData(user);
        var progress = await _context.UserProgresses.FirstOrDefaultAsync(p => p.UserId == user.Id);

        return View(new DashboardViewModel { UserInfo = user, Progress = progress });
    }

    public async Task<IActionResult> Study(string topic, int? unit)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        await SetupSidebarData(user);
        ViewBag.CurrentTopic = string.IsNullOrEmpty(topic) ? "Genel Tekrar" : topic;
        ViewBag.CurrentUnit = unit ?? 1;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SaveProgress([FromBody] ProgressRequest request)
    {
        if (request == null) return BadRequest();

        var existingProgress = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == request.UserId);

        if (existingProgress != null)
        {
            existingProgress.SelectedCourse = request.CourseName;
            existingProgress.WeakPointsJson = JsonSerializer.Serialize(request.WeakPoints);
            _context.UserProgresses.Update(existingProgress);
        }
        else
        {
            _context.UserProgresses.Add(new UserProgress
            {
                UserId = request.UserId,
                SelectedCourse = request.CourseName,
                WeakPointsJson = JsonSerializer.Serialize(request.WeakPoints),
                TotalXP = 0,
                DailyStreak = 1
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class ProgressRequest
{
    public int UserId { get; set; }
    public string CourseName { get; set; }
    public List<string> WeakPoints { get; set; }
}