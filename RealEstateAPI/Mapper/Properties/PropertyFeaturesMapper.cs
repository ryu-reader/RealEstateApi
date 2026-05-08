using RealEstateAPI.Models;

namespace RealEstateAPI.Mapper.Property
{
    public static class PropertyFeaturesMapper
    {


        public static PropertyFeatureResponseDto ToResponseDto(Models.PropertyFeature entity)
        {
            return new PropertyFeatureResponseDto
            {
                Feature = entity.Feature,
                Value = entity.Value
            };
        }



    }
}
