using Microsoft.EntityFrameworkCore;
using Learnicted.Data;
using Learnicted.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1. CONNECTION STRING
// --------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("Connection string NULL geliyor!");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

// --------------------
// 2. AUTH
// --------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "LearnictedAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

// --------------------
// 3. MVC + JSON
// --------------------
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// --------------------
// 4. SERVICES (SAFE)
// --------------------
builder.Services.AddScoped<AiService>();
builder.Services.AddScoped<YouTubeService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --------------------
// 5. ERROR PAGE (SOMEE ÝÇÝN)
// --------------------
app.UseDeveloperExceptionPage();

// --------------------
// 6. HTTPS REDIRECT KAPALI
// --------------------
// app.UseHttpsRedirection();   // Somee’de kapalý býrak

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --------------------
// 7. DB CONNECTION TEST
// --------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.CanConnect();
    }
    catch (Exception ex)
    {
        File.WriteAllText("db_error.txt", ex.ToString());
        throw;
    }
}

// --------------------
// 8. ROUTE
// --------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
