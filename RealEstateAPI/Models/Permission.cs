namespace RealEstateAPI.Models
{

    public enum PermissionType
    {
        ADD_PROPERTY = 0,
        EDIT_PROPERTY = 1,
        DELETE_PROPERTY = 2,
        VIEW_ROLES = 3,
        ADD_ROLE = 4,
        EDIT_ROLE = 5,
        DELETE_ROLE = 6,
        ADD_FEATURE = 7,
        EDIT_FEATURE = 8,
        DELETE_FEATURE = 9
    }

    public class Permission
    {
        public int Id { get; set; }
        public PermissionType Type { get; set; }
        public string Description { get; set; } = String.Empty;
    }




}
