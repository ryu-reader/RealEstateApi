using RealEstateAPI.Models;

namespace RealEstateAPI.DTO.Properties
{
    public class PropertyEditRequestDto
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = String.Empty;

        public string Description { get; set; } = null!;

        public double Price { get; set; }

        public string Currency { get; set; } = null!;

        public string Location { get; set; } = null!;

        public string City { get; set; } = null!;

        public string State { get; set; } = null!;

        public string Country { get; set; } = null!;

        public string Latitude { get; set; } = null!;

        public string Longitude { get; set; } = null!;

        public int Bathrooms { get; set; }

        public int Bedrooms { get; set; }

        public string SQFT { get; set; } = null!;

        public int ParkingSpaces { get; set; } = 0;

        public IFormFile? Image { get; set; }

        public PropertyStatus Status { get; set; }
    }
}
