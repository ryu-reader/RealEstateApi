namespace RealEstateAPI.DTO.Role
{
    public class RoleEditRequestDto
    {
        public string Name { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public int Level { get; set; }
    }
}
