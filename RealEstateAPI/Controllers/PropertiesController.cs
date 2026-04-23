using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;
using RealEstateAPI.Security;
using System.Security.Claims;

namespace RealEstateAPI.Controllers
{

    public class ResponsePagination<T>
    {
        public List<T> Properties { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }



    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {

        private readonly ILogger<PropertiesController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserPermission _userPermission;
        private readonly IWebHostEnvironment _env;

        public PropertiesController(ILogger<PropertiesController> logger, ApplicationDbContext context, UserPermission userPermission, IWebHostEnvironment env)
        {
            _logger = logger;
            _context = context;
            _userPermission = userPermission;
            _env = env;
        }


        [HttpGet]
        public async Task<ActionResult<List<PropertyGet>>> Get(int Page = 1,
            int PageSize = 10,
            string? Name = null,
            string? Code = null,
            ListingType? listingType = null,
            PropertyType? Type = null, PropertyStatus? Status = null)
        {
            

            try
            {

                //pagination
                int pageSize = PageSize;

                var query = _context.Properties.AsQueryable();

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrEmpty(Name)) query = query.Where(p => p.Name.Contains(Name));

                if (!string.IsNullOrEmpty(Code)) query = query.Where(p => p.Code.Contains(Code));


                if (Type != null && Enum.IsDefined(typeof(PropertyType), Type))
                {
                    query = query.Where(p => p.Type == (PropertyType)Type);
                }

                if (Status != null && Enum.IsDefined(typeof(PropertyStatus), Status))
                {
                    query = query.Where(p => p.Status == (PropertyStatus)Status);
                }

                if(listingType != null && Enum.IsDefined(typeof(ListingType), listingType))
                {
                    query = query.Where(p => p.ListingType == (ListingType)listingType);
                }


                var properties = await query
                    .Skip((Page - 1) * pageSize)
                    .Take(pageSize)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new PropertyGet
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code,
                        Description = p.Description,
                        Price = p.Price,
                        Currency = p.Currency,
                        Location = p.Location,
                        City = p.City,
                        State = p.State,
                        Country = p.Country,
                        Latitude = p.Latitude,
                        Longitude = p.Longitude,
                        Bathrooms = p.Bathrooms,
                        Bedrooms = p.Bedrooms,
                        SQFT = p.SQFT,
                        ParkingSpaces = p.ParkingSpaces,
                        
                        ListingType = p.ListingType,
                        Type = p.Type,
                        Image = p.Image,
                        Images = p.Images,
                        Created = p.CreatedBy != null ? p.CreatedBy.Id : 0,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Status = p.Status,
                        Features = p.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                        {
                            Feature = f.Feature,
                            Value = f.Value
                        }).ToList()

                    })
                    .ToListAsync();


                    var response = new ResponsePagination<PropertyGet>
                    {
                        Properties = properties,
                        CurrentPage = Page,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        PageSize = pageSize,
                        TotalCount = properties.Count,
                    };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while retrieving properties. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyGet>> GetById(int id)
        {
            try
            {
                var property = await _context.Properties.Include(p => p.PropertyFeatures)
                    .ThenInclude(e => e.Feature)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (property == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                var PropertyWithCreator = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefaultAsync(p => p.Id == id);


                if(PropertyWithCreator == null) return NotFound(new { message = $"Property with ID {id} not found." });

                var PropertyGet = new PropertyGet
                {
                    Id = PropertyWithCreator.Id,
                    Name = PropertyWithCreator.Name,
                    Code = PropertyWithCreator.Code,
                    Description = PropertyWithCreator.Description,
                    Price = PropertyWithCreator.Price,
                    Currency = PropertyWithCreator.Currency,
                    Location = PropertyWithCreator.Location,
                    City = PropertyWithCreator.City,
                    State = PropertyWithCreator.State,
                    Country = PropertyWithCreator.Country,
                    Latitude = PropertyWithCreator.Latitude,
                    Longitude = PropertyWithCreator.Longitude
                    ,Bathrooms = PropertyWithCreator.Bathrooms,
                    Bedrooms = PropertyWithCreator.Bedrooms,
                    SQFT = PropertyWithCreator.SQFT,
                    ParkingSpaces = PropertyWithCreator.ParkingSpaces,
                    Type = PropertyWithCreator.Type,
                    ListingType = PropertyWithCreator.ListingType,
                    Image = PropertyWithCreator.Image,
                    Images = PropertyWithCreator.Images,
                    Created = PropertyWithCreator.CreatedBy != null ? PropertyWithCreator.CreatedBy.Id : 0,
                    CreatedAt = PropertyWithCreator.CreatedAt,
                    UpdatedAt = PropertyWithCreator.UpdatedAt,
                    Status = PropertyWithCreator.Status,
                    Features = PropertyWithCreator.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()
                };


                return PropertyGet;
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while retrieving the property. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> Create([FromForm] PropertyAddDto property)
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

      

                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.ADD_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to add properties." });
                }


                var existingProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == property.Code);

                if(existingProperty != null)
                {
                    return Conflict(new { message = $"A property with code '{property.Code}' already exists." });
                }

                if (property == null)
                {
                    return BadRequest(new { message = "Property data is required." });
                }

               

                var CreatedByUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                if(CreatedByUser == null)
                {
                    return StatusCode(403, new { message = "User not found." });
                }

                var newProperty = new Property
                {
                    Name = property.Name,
                    Code = property.Code,
                    Description = property.Description,
                    Bathrooms = property.Bathrooms,
                    Bedrooms = property.Bedrooms,
                    ParkingSpaces = property.ParkingSpaces,
                    SQFT = property.SQFT,
                    Price = property.Price,
                    Currency = property.Currency,
                    Location = property.Location,
                    City = property.City,
                    State = property.State,
                    Country = property.Country,
                    Latitude = property.Latitude,
                    Longitude = property.Longitude,
                    Type = property.Type,
                    CreatedBy = CreatedByUser,
                    ListingType = property.ListingType,
                    Status = PropertyStatus.Available
                };

                

                _context.Properties.Add(newProperty);
                await _context.SaveChangesAsync();


                if (property.Image != null && property.Image.Length > 0)
                {
                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(property.Image.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";

                    System.IO.Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", newProperty.Id.ToString()));

                    // Ruta final
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "properties",
                        newProperty.Id.ToString(),
                        randomFileName
                    );

                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await property.Image.CopyToAsync(stream);
                    }

                    // Guardar el nombre en la base de datos

                    var propertyToUpdate = await _context.Properties.FindAsync(newProperty.Id);
                    if (propertyToUpdate != null)
                    {
                        propertyToUpdate.Image = randomFileName;
                        await _context.SaveChangesAsync();
                    }
                }

                var readProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == newProperty.Id);


                if( readProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {newProperty.Id} not found after creation." });
                }

                var PropertyGet = new PropertyGet
                {
                    Id = readProperty.Id,
                    Name = readProperty.Name,
                    Code = readProperty.Code,
                    Description = readProperty.Description,
                    Price = readProperty.Price,
                    Currency = readProperty.Currency,
                    Location = readProperty.Location,
                    City = readProperty.City,
                    State = readProperty.State,
                    Country = readProperty.Country,
                    Latitude = readProperty.Latitude,
                    Longitude = readProperty.Longitude,
                    Bathrooms = readProperty.Bathrooms,
                    Bedrooms = readProperty.Bedrooms,
                    SQFT = readProperty.SQFT,
                    ParkingSpaces = readProperty.ParkingSpaces,
                    ListingType = readProperty.ListingType,
                    Status = readProperty.Status,
                    Type = readProperty.Type,
                    Image = readProperty.Image,
                    Images = readProperty.Images,
                    Created = readProperty.CreatedBy != null ? readProperty.CreatedBy.Id : 0,
                    CreatedAt = readProperty.CreatedAt,
                    UpdatedAt = readProperty.UpdatedAt,
                    
                    Features = readProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetById), new { id = PropertyGet.Id }, PropertyGet);


            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while creating the property. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> Update(int id, [FromForm] PropertyUpdateDto property)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }


                var existingProperty = await _context.Properties.FindAsync(id);
                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }


                //Extra Verification to avoid duplicate code when updating
                var existingPropertyWithSameCode = await _context.Properties.FirstOrDefaultAsync(p => p.Code == property.Code && p.Id != id);
                if (existingPropertyWithSameCode != null)
                {
                    return Conflict(new { message = $"A property with code '{property.Code}' already exists." });
                }

                var HasCreated = _context.Properties.
                    Include(p => p.CreatedBy)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefault(p => p.Id == id);


                var MyUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                if(MyUser == null)
                {
                    return StatusCode(403, new { message = "User not found." });
                }

                bool CanEdit = HasCreated != null && HasCreated.CreatedBy != null && HasCreated.CreatedBy.Id != Convert.ToInt32(userId) && !_userPermission.VerifiedRoleLevel(MyUser.Id, HasCreated.CreatedBy.Role.Level);


                if (CanEdit)
                {
                    return StatusCode(403, new { message = "You can`t edit this property" });
                }



                existingProperty.Name = property.Name ?? existingProperty.Name;

                existingProperty.Code = property.Code ?? existingProperty.Code;

                existingProperty.Description = property.Description ?? existingProperty.Description;

                if (property.Price != 0)
                {
                    existingProperty.Price = property.Price;
                }

                existingProperty.Currency = property.Currency ?? existingProperty.Currency;
                existingProperty.Location = property.Location ?? existingProperty.Location;
                existingProperty.City = property.City ?? existingProperty.City;
                existingProperty.State = property.State ?? existingProperty.State;
                existingProperty.Country = property.Country ?? existingProperty.Country;
                existingProperty.Latitude = property.Latitude ?? existingProperty.Latitude;
                existingProperty.Longitude = property.Longitude ?? existingProperty.Longitude;
                existingProperty.UpdatedAt = DateTime.UtcNow;
                existingProperty .Bathrooms = property.Bathrooms != 0 ? property.Bathrooms : existingProperty.Bathrooms;
                existingProperty.Bedrooms = property.Bedrooms != 0 ? property.Bedrooms : existingProperty.Bedrooms;
                existingProperty.SQFT = property.SQFT ?? existingProperty.SQFT;
                existingProperty.ParkingSpaces = property.ParkingSpaces != 0 ? property.ParkingSpaces : existingProperty.ParkingSpaces;
                existingProperty.ListingType = property.ListingType ;

                existingProperty.Status = property.Status;


                if (property.Type != null) existingProperty.Type = property.Type.Value;



                //Update Image
                if (property.Image != null && property.Image.Length > 0)
                {
                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(property.Image.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";


                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString());
                    string pathToExistingImage = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString(), existingProperty.Image ?? "");

                    if (Directory.Exists(folderPath) && System.IO.File.Exists(pathToExistingImage) && !String.IsNullOrEmpty(existingProperty.Image))
                    {
                        System.IO.File.Delete(pathToExistingImage);
                    }

                    if (!Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString()));
                    }

                    // Ruta final
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "properties",
                        existingProperty.Id.ToString(),
                        randomFileName
                    );

                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await property.Image.CopyToAsync(stream);
                    }

                    // Guardar el nombre en la base de datos

                    var propertyToUpdate = await _context.Properties.FindAsync(existingProperty.Id);
                    if (propertyToUpdate != null)
                    {
                        propertyToUpdate.Image = randomFileName;
                        await _context.SaveChangesAsync();
                    }
                }

                var updatedProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == existingProperty.Id);


                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {existingProperty.Id} not found after update." });

                var PropertyGet = new PropertyGet
                    {
                        Id = updatedProperty.Id,
                        Name = updatedProperty.Name,
                        Code = updatedProperty.Code,
                        Description = updatedProperty.Description,
                        Price = updatedProperty.Price,
                        Currency = updatedProperty.Currency,
                        Location = updatedProperty.Location,
                        City = updatedProperty.City,
                        State = updatedProperty.State,
                        Country = updatedProperty.Country,
                        Latitude = updatedProperty.Latitude,
                        Longitude = updatedProperty.Longitude,
                        Bathrooms = updatedProperty.Bathrooms,
                        Bedrooms = updatedProperty.Bedrooms,
                        SQFT = updatedProperty.SQFT,
                        ParkingSpaces = updatedProperty.ParkingSpaces,
                        ListingType = updatedProperty.ListingType,
                        Type = updatedProperty.Type,
                        Image = updatedProperty.Image,
                        Images = updatedProperty.Images,
                        Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                        CreatedAt = updatedProperty.CreatedAt,
                        UpdatedAt = updatedProperty.UpdatedAt,
                        Status = updatedProperty.Status,
                        Features = updatedProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                        {
                            Feature = f.Feature,
                            Value = f.Value
                        }).ToList()
                };




                await _context.SaveChangesAsync();
                return Ok(PropertyGet);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpPut]
        [Route("update-images/{id}")]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> UpdateImages(int id, [FromForm] List<IFormFile>  Images)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }

                var existingProperty = await _context.Properties
                    .FindAsync(id);

                if(existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString());

                if (!Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }


                foreach (var item in existingProperty.Images)
                {
                    //Delete Image
                    string pathToExistingImage = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString(), item);
                    if (System.IO.File.Exists(pathToExistingImage))
                    {
                        System.IO.File.Delete(pathToExistingImage);
                    }
                }


                existingProperty.Images.Clear();
                _context.SaveChanges();
                

                List<string> ImagesList = new List<string>();

                foreach (var image in Images)
                {

                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(image.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";


                   


                    // Ruta final
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "properties",
                        existingProperty.Id.ToString(),
                        randomFileName
                    );

                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    ImagesList.Add(randomFileName);

                }


                existingProperty.Images.AddRange(ImagesList);
                _context.SaveChanges();

                var updatedProperty = await _context.Properties.FindAsync(id);

                if(updatedProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found after updating images." });
                }

                var PropertyGet = new PropertyGet
                {
                    Id = updatedProperty.Id,
                    Name = updatedProperty.Name,
                    Code = updatedProperty.Code,
                    Description = updatedProperty.Description,
                    Price = updatedProperty.Price,
                    Currency = updatedProperty.Currency,
                    Location = updatedProperty.Location,
                    City = updatedProperty.City,
                    State = updatedProperty.State,
                    Country = updatedProperty.Country,
                    Latitude = updatedProperty.Latitude,
                    Longitude = updatedProperty.Longitude,
                    Bathrooms = updatedProperty.Bathrooms,
                    Bedrooms = updatedProperty.Bedrooms,
                    SQFT = updatedProperty.SQFT,
                    ListingType = updatedProperty.ListingType,
                    ParkingSpaces = updatedProperty.ParkingSpaces,
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt,
                    Status = updatedProperty.Status,
                    Features = updatedProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()
                };


                return Ok(PropertyGet);

            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property images. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpPost]
        [Route("update-images-position/{id}")]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> UpdatePositionImages(int id, int oldIndex, int newIndex)
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }


                if (oldIndex == newIndex)
                {
                    return BadRequest(new { message = "Old index and new index cannot be the same." });
                }

                

                var existingProperty = await _context.Properties
                    .FindAsync(id);
                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }
                if (oldIndex < 0 || oldIndex >= existingProperty.Images.Count || newIndex < 0 || newIndex >= existingProperty.Images.Count)
                {
                    return BadRequest(new { message = "Invalid image indices." });
                }
                var imageToMove = existingProperty.Images[oldIndex];
                existingProperty.Images.RemoveAt(oldIndex);
                existingProperty.Images.Insert(newIndex, imageToMove);
                _context.SaveChanges();
                var updatedProperty = await _context.Properties.FindAsync(id);

                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {id} not found after updating images." });


                var PropertyGet = new PropertyGet
                {
                    Id = updatedProperty.Id,
                    Name = updatedProperty.Name,
                    Code = updatedProperty.Code,
                    Description = updatedProperty.Description,
                    Price = updatedProperty.Price,
                    Currency = updatedProperty.Currency,
                    Location = updatedProperty.Location,
                    City = updatedProperty.City,
                    State = updatedProperty.State,
                    Country = updatedProperty.Country,
                    Latitude = updatedProperty.Latitude,
                    Longitude = updatedProperty.Longitude,
                    Bathrooms = updatedProperty.Bathrooms,
                    Bedrooms = updatedProperty.Bedrooms,
                    SQFT = updatedProperty.SQFT,
                    ParkingSpaces = updatedProperty.ParkingSpaces,
                    ListingType = updatedProperty.ListingType,
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt,
                    Status = updatedProperty.Status,
                    Features = updatedProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()
                };


                return Ok(PropertyGet);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property images. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpPost]
        [Route("update-features/{id}")]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> UpdateFeatures(int id, [FromBody] List<PropertyFeatureAddDto> features)
        {
            try
            {

               
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }

                var idExistingProperty = await _context.Properties
                    .Include(p => p.PropertyFeatures)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (idExistingProperty == null)
                    {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                // Remove existing features
                _context.PropertyFeatures.RemoveRange(idExistingProperty.PropertyFeatures);


                

                foreach (var featureDto in features)
                {

                    var feature = _context.Features.Find(featureDto.FeatureId);

                    if (feature == null) continue;

                    PropertyFeature propertyFeature = new PropertyFeature
                    {
                        Property = idExistingProperty,
                        Feature = feature,
                        Value = featureDto.Value,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.PropertyFeatures.Add(propertyFeature);


                }

                await _context.SaveChangesAsync();

                var updatedProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {id} not found after updating features." });

                var PropertyGet = new PropertyGet
                {
                    Id = updatedProperty.Id,
                    Name = updatedProperty.Name,
                    Code = updatedProperty.Code,
                    Description = updatedProperty.Description,
                    Price = updatedProperty.Price,
                    Currency = updatedProperty.Currency,
                    Location = updatedProperty.Location,
                    City = updatedProperty.City,
                    State = updatedProperty.State,
                    Country = updatedProperty.Country,
                    Latitude = updatedProperty.Latitude,
                    Longitude = updatedProperty.Longitude,
                    Bathrooms = updatedProperty.Bathrooms,
                    Bedrooms = updatedProperty.Bedrooms,
                    SQFT = updatedProperty.SQFT,
                    ParkingSpaces = updatedProperty.ParkingSpaces,
                    Type = updatedProperty.Type,
                    ListingType = updatedProperty.ListingType,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt,
                    Status = updatedProperty.Status,
                    Features = updatedProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()
                };

                return Ok(PropertyGet);


            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property features. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }


        [HttpDelete]
        [Route("delete-image/{id}/{imageIndex}")]
        [Authorize]
        public async Task<ActionResult<PropertyGet>> DeleteImage(int id, int imageIndex)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }
                var existingProperty = await _context.Properties
                    .FindAsync(id);
                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }
                if (imageIndex < 0 || imageIndex >= existingProperty.Images.Count)
                {
                    return BadRequest(new { message = "Invalid image index." });
                }
                var imageToDelete = existingProperty.Images[imageIndex];
                string pathToExistingImage = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString(), imageToDelete);
                if (System.IO.File.Exists(pathToExistingImage))
                {
                    System.IO.File.Delete(pathToExistingImage);
                }
                existingProperty.Images.RemoveAt(imageIndex);
                _context.SaveChanges();
                var updatedProperty = await _context.Properties.FindAsync(id);

                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {id} not found after deleting image." });

                var PropertyGet = new PropertyGet
                {
                    Id = updatedProperty.Id,
                    Name = updatedProperty.Name,
                    Code = updatedProperty.Code,
                    Description = updatedProperty.Description,
                    Price = updatedProperty.Price,
                    Currency = updatedProperty.Currency,
                    Location = updatedProperty.Location,
                    City = updatedProperty.City,
                    State = updatedProperty.State,
                    Country = updatedProperty.Country,
                    Latitude = updatedProperty.Latitude,
                    Longitude = updatedProperty.Longitude,
                    Bathrooms = updatedProperty.Bathrooms,
                    Bedrooms = updatedProperty.Bedrooms,
                    SQFT = updatedProperty.SQFT,
                    ParkingSpaces = updatedProperty.ParkingSpaces,
                    ListingType = updatedProperty.ListingType,
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt,
                    Status = updatedProperty.Status,
                    Features = updatedProperty.PropertyFeatures.Select(f => new PropertyFeatureResponseDto
                    {
                        Feature = f.Feature,
                        Value = f.Value
                    }).ToList()

                };


                return Ok(PropertyGet);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while deleting the property image. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }


        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.DELETE_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to delete properties." });
                }

                var existingProperty = await _context.Properties.FindAsync(id);
                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString());

                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }


                _context.Properties.Remove(existingProperty);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while deleting the property. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }

        [HttpGet]
        [Route("view-image/{id}/{filename}")]
        public IActionResult ViewImage(int id, string filename)
        {
            var ruta = Path.Combine(_env.WebRootPath, "images", "properties", id.ToString(), filename);
            if (!System.IO.File.Exists(ruta))
            {
                return NotFound(new { message = $"Image '{filename}' for property with ID {id} not found." });
            }
            var imageBytes = System.IO.File.ReadAllBytes(ruta);
            var contentType = GetContentTypeImage(ruta);
            return File(imageBytes, contentType);
        }

        // Agrega este método privado en la clase PropertiesController para solucionar CS0103
        private string GetContentTypeImage(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}
