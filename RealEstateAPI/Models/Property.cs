using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstateAPI.Models
{


    public class PropertyAddDto
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

    public class PropertyUpdateDto
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

        public PropertyType? Type { get; set; } = null;

        public ListingType ListingType { get; set; }

        public IFormFile? Image { get; set; }

        public PropertyStatus Status { get; set; }

    }

    


    public enum ListingType
    {
        Sale,
        Rent
    }

    public enum PropertyType
    {
        Apartment = 0,
        House = 1,
        Condo = 2,
        Townhouse = 3,
        Land = 4
    }

    public enum PropertyStatus
    {
        Available = 0,
        Sold = 1,
        Pending = 2
    }


    public class PropertyGet
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

    }

    public class Property
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public User CreatedBy { get; set; } = null!;

        public PropertyStatus Status { get; set; } = PropertyStatus.Available;

        public List<PropertyFeature> PropertyFeatures { get; set; } = new();

    }
}
