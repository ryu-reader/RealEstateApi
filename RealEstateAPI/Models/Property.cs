namespace RealEstateAPI.Models
{


    public class PropertyAddDto
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

        public PropertyType Type { get; set; }

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

        public PropertyType? Type { get; set; } = null;

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
        Apartment,
        House,
        Condo,
        Townhouse,
        Land
    }

    public enum PropertyStatus
    {
        Available,
        Sold,
        Pending
    }


    public class PropertyFull
    {
        public Property Property { get; set; } = null!;

        public List<string> Images { get; set; } = null!;
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

        public PropertyType? Type { get; set; } = null;

        public string? Image { get; set; } = null;

        public List<string> Images { get; set; } = new();

        public int Created { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public PropertyStatus Status { get; set; }
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

        public PropertyType? Type { get; set; } = null;

        public string? Image { get; set; } = null;

        public List<string> Images { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = null;

        public User CreatedBy { get; set; } = null!;

        public PropertyStatus Status { get; set; } = PropertyStatus.Available;


    }
}
