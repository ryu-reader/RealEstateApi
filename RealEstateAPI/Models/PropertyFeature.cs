namespace RealEstateAPI.Models
{

    public class PropertyFeatureResponseDto
    {
        public Feature Feature { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class PropertyFeature
    {
        public int Id { get; set; }
        public Property Property { get; set; } = null!;
        public Feature Feature { get; set; } = null!;
        public string Value { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
