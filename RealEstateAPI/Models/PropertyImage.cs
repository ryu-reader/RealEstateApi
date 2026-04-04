namespace RealEstateAPI.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        public Property Property { get; set; } = null!;

        public List<string> Images { get; set;} = null!;  
    }
}
