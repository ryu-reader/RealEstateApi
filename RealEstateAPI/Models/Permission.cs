namespace RealEstateAPI.Models
{

    public enum PermissionType
    {
        ADD_PROPERTY,
        EDIT_PROPERTY,
        DELETE_PROPERTY,
    }

    public class Permission
    {
        public int Id { get; set; }
        public PermissionType Type { get; set; }
        public string Description { get; set; } = String.Empty;
    }




}
