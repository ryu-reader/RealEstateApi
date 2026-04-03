namespace RealEstateAPI.Models
{

    public class LoginDTO
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class TokenRequest
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class UserAddDTO
    {
        public string Name { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;

        public string Username { get; set; } = String.Empty;

        public string Password { get; set; } = String.Empty;

        public string RepeatPassword { get; set; } = String.Empty;

        public IFormFile? Image { get; set; } // opcional


    }

    public class User
    {

        public int Id { get; set; }

        public string Name { get; set; } = String.Empty;

        public string Email { get; set; } = String.Empty;

        public string Username { get; set; } = String.Empty;

        public string Password { get; set; } = String.Empty;

        public string Image { get; set; } = String.Empty;

        public Role Role { get; set; } = null!;

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }


    }
}
