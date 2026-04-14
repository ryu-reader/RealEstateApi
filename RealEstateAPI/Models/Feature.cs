namespace RealEstateAPI.Models
{


    public class FeatureAddDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IFormFile? Icon { get; set; }
    }

    public class FeatureUpdateDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public IFormFile? Icon { get; set; }
    }

    public class Feature
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;

        public string Icon { get; set; } = null!;


    }
}
