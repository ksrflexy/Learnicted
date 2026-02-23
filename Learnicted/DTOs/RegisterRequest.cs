namespace Learnicted.DTOs;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
