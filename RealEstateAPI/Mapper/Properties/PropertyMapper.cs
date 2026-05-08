using RealEstateAPI.DTO.Properties;
using RealEstateAPI.Mapper.Property;
using RealEstateAPI.Models;

namespace RealEstateAPI.Mapper.Properties
{
    public static class PropertyMapper
    {

        
        public static Models.Property ToEntity(PropertyRequestDto dto)
        {
            return new Models.Property
            {
                Name = dto.Name,
                Code = dto.Code,
                Description = dto.Description,
                Price = dto.Price,
                Currency = dto.Currency,
                Location = dto.Location,
                City = dto.City,
                State = dto.State,
                Country = dto.Country,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Bathrooms = dto.Bathrooms,
                Bedrooms = dto.Bedrooms,
                SQFT = dto.SQFT,
                ParkingSpaces = dto.ParkingSpaces,
                Type = dto.Type,
                ListingType = dto.ListingType,
                Image = dto.Image?.FileName ?? string.Empty,
            };
        }
        
     

        public static PropertyResponseDto ToResponseDto(Models.Property entity)
        {
            return new PropertyResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                Description = entity.Description,
                Price = entity.Price,
                Currency = entity.Currency,
                Location = entity.Location,
                City = entity.City,
                State = entity.State,
                Country = entity.Country,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                Bathrooms = entity.Bathrooms,
                Bedrooms = entity.Bedrooms,
                SQFT = entity.SQFT,
                ParkingSpaces = entity.ParkingSpaces,
                Type = entity.Type,
                ListingType = entity.ListingType,
                Image = entity.Image,
                Images = entity.Images ?? new List<string>(),
                Created = entity.CreatedBy?.Id ?? 0,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Status = entity.Status,
                Features = entity.PropertyFeatures.Select(f => PropertyFeaturesMapper.ToResponseDto(f)).ToList(),
                Owner = entity.Owner
            };
        }

     
    }
}
