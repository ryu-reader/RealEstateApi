namespace RealEstateAPI.DTO.User
{
    public class UserRequestAddDTO
    {
        public string Name { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;

        public string Username { get; set; } = String.Empty;

        public string Password { get; set; } = String.Empty;

        public string RepeatPassword { get; set; } = String.Empty;

        public IFormFile? Image { get; set; } // opcional
    }
}
