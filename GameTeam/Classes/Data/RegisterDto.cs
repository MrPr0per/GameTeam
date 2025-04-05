namespace GameTeam.Classes.Data
{
    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
    }

    public class LoginDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public required string Password { get; set; }
    }
}
