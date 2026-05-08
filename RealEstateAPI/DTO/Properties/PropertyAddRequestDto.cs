using RealEstateAPI.Models;

namespace RealEstateAPI.DTO.Properties
{
    public class PropertyRequestDto
    {
        public string Name { get; set; } = null!;

        public string Code { get; set; } = String.Empty;

        public int Bathrooms { get; set; }

        public int Bedrooms { get; set; }

        public int ParkingSpaces { get; set; } = 0;

        public string SQFT { get; set; } = null!;


        public string Description { get; set; } = null!;

        public double Price { get; set; }

        public string Currency { get; set; } = null!;

        public string Location { get; set; } = null!;

        public string City { get; set; } = null!;

        public string State { get; set; } = null!;

        public string Country { get; set; } = null!;

        public string Latitude { get; set; } = null!;

        public string Longitude { get; set; } = null!;

        public PropertyType Type { get; set; }

        public ListingType ListingType { get; set; }

        public IFormFile? Image { get; set; }
    }
}
