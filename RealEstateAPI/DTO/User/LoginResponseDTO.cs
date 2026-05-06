using RealEstateAPI.Models; // Asegúrate de que este using apunte al namespace correcto donde está definido el tipo Role

namespace RealEstateAPI.DTO.User
{

   

    public class LoginResponseDTO
    {

    
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserResponseDTO User { get; set; } = null!;

        public List<PermissionType > Permissions { get; set; } = new();

        public string Role { get; set; } = string.Empty;

        public int UserId { get; set; }

    }
}
