namespace RealEstateAPI.Models
{

    public class RoleAddDTO
    {
        public string Name { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public int Level { get; set; }
    }


    public class Role
    {

        public int Id { get; set; }

        public string Name { get; set; } = String.Empty;

        public string Description { get; set; } = String.Empty;

        public int Level { get; set; }

        public List<PermissionRole> PermissionsRole { get; set; } = new();

    }
}
