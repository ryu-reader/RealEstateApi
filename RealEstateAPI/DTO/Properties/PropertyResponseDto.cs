using RealEstateAPI.Models;

namespace RealEstateAPI.DTO.Properties
{
    public class PropertyResponseDto
    {
        public int Id { get; set; }

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

        public PropertyType? Type { get; set; } = null;

        public ListingType ListingType { get; set; }

        public string? Image { get; set; } = null;

        public List<string> Images { get; set; } = new();

        public int Created { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public PropertyStatus Status { get; set; }

        public List<PropertyFeatureResponseDto> Features { get; set; } = new();

        public Models.Owner? Owner { get; set; } = null!;


    }
}
