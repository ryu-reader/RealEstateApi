namespace RealEstateAPI.Models
{

    public class UserAddDTO
    {
        public string Name { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;

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


    }
}
