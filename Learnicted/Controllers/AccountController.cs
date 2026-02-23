using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Learnicted.Data;
using Learnicted.Models;
using Learnicted.DTOs; // DTO'ları kullanabilmek için ekledik
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Learnicted.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context) { _context = context; }

        public IActionResult Login() => View();

        // --- KAYIT OLMA (DTO İLE GÜNCELLENDİ) ---
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null) return BadRequest("Veriler alınamadı.");

            // Email mükerrer kontrolü
            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (exists) return BadRequest("Bu e-posta adresi zaten kullanımda.");

            // DTO'daki verileri asıl User modeline eşliyoruz
            var newUser = new User
            {
                FullName = request.FullName,
                UserName = request.UserName,
                Email = request.Email,
                Phone = request.Phone,
                Grade = request.Grade,
                Password = request.Password, // Gerçek senaryoda şifre hashlenmelidir
                CreatedDate = DateTime.Now,
                IsPlus = false,
                Streak = 0,
                Bio = "Öğrenmeye yeni başladı!",
                GithubUsername = ""
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Kayıt sonrası otomatik giriş yap
                await SignInUser(newUser, false);

                return Ok();
            }
            catch (Exception ex)
            {
                // InnerException detayını Somee üzerinde görmek için ex.Message ekledik
                return BadRequest("Kayıt hatası: " + ex.Message);
            }
        }

        // --- GİRİŞ YAPMA (DTO İLE GÜNCELLENDİ) ---
        [HttpPost]
        public async Task<IActionResult> LoginAction([FromBody] LoginRequest loginData)
        {
            if (loginData == null) return BadRequest("Giriş bilgileri eksik.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginData.Email && u.Password == loginData.Password);

            if (user == null) return Unauthorized("Hatalı e-posta veya şifre.");

            await SignInUser(user, loginData.RememberMe);

            return Ok(new { userId = user.Id, userName = user.UserName });
        }

        // Oturum açma (Cookie oluşturma)
        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("FullName", user.FullName ?? user.UserName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}