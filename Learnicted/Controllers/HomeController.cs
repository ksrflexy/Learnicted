using System.Diagnostics;
using System.Security.Claims;
using Learnicted.Models;
using Microsoft.AspNetCore.Mvc;
using Learnicted.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Learnicted.Controllers
{
    [Authorize] // Sadece giriþ yapanlar görebilir
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context) { _context = context; }

        public IActionResult Index() => View();

        // --- YENÝ EKLENEN METOD: Remediation Sayfasýný Açar ---
        public IActionResult Remediation(string topic)
        {
            // Eðer topic boþ gelirse ana sayfaya veya profile gönder
            if (string.IsNullOrEmpty(topic)) return RedirectToAction("Profile");

            // URL'den gelen konuyu (topic) sayfaya (View) taþýr
            ViewBag.CurrentTopic = topic;
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            // 1. Çerezden kullanýcý ID'sini al
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(userIdStr);

            // 2. Sadece bu kullanýcýyý getir
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (user == null) return RedirectToAction("Login", "Account");

            // ViewBag üzerinden ID gönderelim (LocalStorage key için gerekebilir)
            ViewBag.UserId = currentUserId;

            // 3. Veritabaný istatistiklerini bu kullanýcýya göre çek
            int completedUnits = await _context.UserUnits.CountAsync(u => u.UserId == currentUserId && u.IsCompleted);
            int activeCourses = await _context.UserCourses.CountAsync(c => c.UserId == currentUserId && !c.IsFinished);
            var missingTopics = await _context.UserRemediations
                .Where(r => r.UserId == currentUserId && !r.IsSolved)
                .Select(r => r.TopicName).ToListAsync();

            var model = new UserProfileViewModel
            {
                FullName = user.FullName ?? user.UserName,
                UserName = user.UserName,
                Bio = user.Bio,
                GithubUsername = user.GithubUsername,
                CompletedUnitsText = $"{completedUnits} Ünite",
                ActiveCoursesText = $"{activeCourses} Kurs",
                SuccessPointsText = $"{completedUnits * 100} XP",
                MissingTopics = missingTopics,
                Projects = new List<ProjectModel>()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateModel data)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            int currentUserId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user != null && data != null)
            {
                user.Bio = data.Bio;
                user.GithubUsername = data.GithubUsername;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            return NotFound();
        }
    }

    public class UserProfileUpdateModel
    {
        public string Bio { get; set; }
        public string GithubUsername { get; set; }
    }
}