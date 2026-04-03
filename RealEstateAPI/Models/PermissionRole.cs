namespace RealEstateAPI.Models
{
    public class PermissionRole
    {

        public int Id { get; set; }

        public Role Role { get; set; } = null!;

        public Permission Permission { get; set; } = null!;

    }
}
