namespace RealEstateAPI.DTO.User
{
    public class UserAddRequestDto
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
