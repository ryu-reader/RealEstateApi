namespace RealEstateAPI.DTO.User
{
    public class UserResponseDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public string Image { get; set; }

        public Models.Role Role { get; set; }
        public string Username { get; internal set; }
    }
}
