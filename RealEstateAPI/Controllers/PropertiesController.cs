using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.DTO;
using RealEstateAPI.DTO.Properties;
using RealEstateAPI.Mapper.Properties;
using RealEstateAPI.Models;
using RealEstateAPI.Security;
using System.Security.Claims;

namespace RealEstateAPI.Controllers
{

   



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

        private bool ExistProperty(int id)
        {
            return _context.Properties.Any(p => p.Id == id);
        }


        private void UpdateImageProperty(Property property, IFormFile image)
        {
            try
            {
                if (image != null && image.Length > 0)
                {
                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(image.FileName);
                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";
                    System.IO.Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", property.Id.ToString()));
                    // Ruta final
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "properties",
                        property.Id.ToString(),
                        randomFileName
                    );
                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    // Guardar el nombre en la base de datos
                    var propertyToUpdate = _context.Properties.Find(property.Id);
                    if (propertyToUpdate != null)
                    {
                        propertyToUpdate.Image = randomFileName;
                        _context.SaveChanges();
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the property image.");
            }
        }


        private List<string> UpdateImagesProperty ( List<IFormFile> images, int propertyId)
        {
            List<string> ImagesList = new List<string>();
            try
            {

                foreach (var image in images)
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
                        propertyId.ToString(),
                        randomFileName
                    );
                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        image.CopyTo(stream);
                    }
                    ImagesList.Add(randomFileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the property images.");
            }
            return ImagesList;
        }


        private void DeleteImageFile(int id, string Filename) {

            try
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", id.ToString());

                if (System.IO.Directory.Exists(folderPath))
                {

                    string file = Path.Combine(folderPath, Filename);

                    var existingImagePath = Path.Combine(folderPath, Filename);
                    if (System.IO.File.Exists(existingImagePath))
                    {
                        System.IO.File.Delete(existingImagePath);
                    }

                }


            }
            catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }
        
        }


        private void UpdateProperty(int id, PropertyEditRequestDto property)
        {
            try
            {
                var existingProperty = _context.Properties.Find(id);
                if (existingProperty != null)
                {
                    existingProperty.Name = property.Name;
                    existingProperty.Code = property.Code;
                    existingProperty.Description = property.Description;
                    existingProperty.Price = property.Price;
                    existingProperty.Currency = property.Currency;
                    existingProperty.Location = property.Location;
                    existingProperty.City = property.City;
                    existingProperty.State = property.State;
                    existingProperty.Country = property.Country;
                    existingProperty.Latitude = property.Latitude;
                    existingProperty.Longitude = property.Longitude;
                    existingProperty.Bathrooms = property.Bathrooms;
                    existingProperty.Bedrooms = property.Bedrooms;
                    existingProperty.SQFT = property.SQFT;
                    existingProperty.ParkingSpaces = property.ParkingSpaces;
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
        

        [HttpGet]
        public async Task<ActionResult<List<PropertyResponseDto>>> Get(int Page = 1,
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

                query = query.Include(x => x.Owner);


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

                var totalCount = await query.CountAsync();


                var properties = await query
                    .Skip((Page - 1) * pageSize)
                    .Take(pageSize)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => PropertyMapper.ToResponseDto(p))
                    .ToListAsync();


                    var response = new ResponsePagination<PropertyResponseDto>
                    {
                        Data = properties,
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
        public async Task<ActionResult<PropertyResponseDto>> GetById(int id)
        {
            try
            {
                var property = await _context.Properties
                    .Include(c => c.CreatedBy)
                    .Include(o => o.Owner)
                    .Include(p => p.PropertyFeatures)
                    .ThenInclude(e => e.Feature)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (property == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                var PropertyResponse = PropertyMapper.ToResponseDto(property);

                return PropertyResponse;
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
        public async Task<ActionResult<PropertyResponseDto>> Create([FromForm] PropertyRequestDto property)
        {
            try
            {
                if (property == null) return BadRequest(new { message = "Property data is required." });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.ADD_PROPERTY);
                if (!permission) return StatusCode(403, new { message = "You do not have permission to add properties." });
               
                var existingProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Code == property.Code);

                if(existingProperty != null)
                {
                    return Conflict(new { message = $"A property with code '{property.Code}' already exists." });
                }

     
                var CreatedByUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                if(CreatedByUser == null) return StatusCode(403, new { message = "User not found." });
                

                var newProperty = PropertyMapper.ToEntity(property);

                newProperty.CreatedBy = CreatedByUser;
                newProperty.Status = PropertyStatus.Available;
                _context.Properties.Add(newProperty);
                await _context.SaveChangesAsync();


                if (property.Image != null && property.Image.Length > 0) UpdateImageProperty(newProperty, property.Image);


                var readProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == newProperty.Id);


                if( readProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {newProperty.Id} not found after creation." });
                }

                var propertyResponse = PropertyMapper.ToResponseDto(readProperty);

                return CreatedAtAction(nameof(GetById), new { id = propertyResponse.Id }, propertyResponse);

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
        public async Task<ActionResult<PropertyResponseDto>> Update(int id, [FromForm] PropertyEditRequestDto property)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission)
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                

             
                var existingProperty = await _context.Properties
                    .Include(c => c.CreatedBy)
                    .FirstOrDefaultAsync(w => w.Id ==  id);


                if (existingProperty == null) 
                    return NotFound(new { message = $"Property with ID {id} not found." });



                if(existingProperty.ListingType == ListingType.Sale && existingProperty.Status == PropertyStatus.Sold)
                {
                    return Conflict(new { message = $"Property with ID {id} sold, Date sold: {existingProperty.UpdatedAt.ToString()}" });
                }
                


                //Extra Verification to avoid duplicate code when updating
                var existingPropertyWithSameCode = await _context.Properties.FirstOrDefaultAsync(p => p.Code == property.Code && p.Id != id);
                if (existingPropertyWithSameCode != null) 
                    return Conflict(new { message = $"A property with code '{property.Code}' already exists." });



                var MyUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                if(MyUser == null)
                    return StatusCode(403, new { message = "User not found." });


                bool CanEdit = false;


                if(existingProperty.CreatedBy != null)
                {
                    CanEdit = _userPermission.HasSuperiorRoleTo(Convert.ToInt32(userId), existingProperty.CreatedBy.Id);
                }


                if (!CanEdit)
                    return StatusCode(403, new { message = "You can`t edit this property" });
                

                UpdateProperty(id, property);


                //Update Image
                if (property.Image != null && !string.IsNullOrEmpty(existingProperty.Image) && property.Image.Length > 0)
                {
                    DeleteImageFile(id, existingProperty.Image);
                    UpdateImageProperty(existingProperty, property.Image);
                }


                var updatedProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == existingProperty.Id);


                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {existingProperty.Id} not found after update." });

                var PropertyGet = PropertyMapper.ToResponseDto(updatedProperty);


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
        public async Task<ActionResult<PropertyResponseDto>> UpdateImages(int id, [FromForm] List<IFormFile>  Images)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission) return StatusCode(403, new { message = "You do not have permission to edit properties." });
                

                var existingProperty = await _context.Properties
                    .FindAsync(id);

                if(existingProperty == null) return NotFound(new { message = $"Property with ID {id} not found." });
                

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

                ImagesList = UpdateImagesProperty(Images, existingProperty.Id);


                existingProperty.Images.AddRange(ImagesList);
                _context.SaveChanges();

                var updatedProperty = await _context.Properties.FindAsync(id);

                if(updatedProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found after updating images." });
                }

                var response = PropertyMapper.ToResponseDto(updatedProperty);


                return Ok(response);

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
        public async Task<ActionResult<PropertyResponseDto>> UpdateFeatures(int id, [FromBody] List<PropertyFeatureAddDto> features)
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

                var response = PropertyMapper.ToResponseDto(updatedProperty);


                return Ok(response);


            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property features. Please try again later. " + ex.Message,
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



        [HttpPost]
        [Route("mark-as-pending/{id}")]
        [Authorize]
        public async Task<ActionResult<PropertyResponseDto>> MarkAsPending(int id)
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
                existingProperty.Status = PropertyStatus.Pending;
                await _context.SaveChangesAsync();
                var updatedProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if(updatedProperty == null) return NotFound(new { message = $"Property with ID {id} not found after marking as pending." });


                var response = PropertyMapper.ToResponseDto(updatedProperty);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while marking the property as pending. Please try again later. " + ex.Message
                );
            }
        }

        [HttpPost]
        [Route("sale/reset/{id}")]
        [Authorize]
        public async Task<ActionResult> ResetSaleStatus(int id, [FromBody] SaleResetPropertyDto saleResetDTO)
        {

            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_PROPERTY);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit properties." });
                }

                var existingProperty = await _context.Properties.Where(e => e.Id == id)
                    .Include(o => o.Owner)
                    .FirstOrDefaultAsync();
                    
                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }


                var currentOwner = existingProperty.Owner;

                var countOfPropertiesWithSameOwner = 0;

                if (currentOwner != null)
                {
                    countOfPropertiesWithSameOwner = _context.Properties
                        .Where(x => x.Owner != null && x.Owner.Id == currentOwner.Id)
                        .Count();
                }

                existingProperty.Owner = null;

                if (saleResetDTO.DeleteOwner && currentOwner != null && countOfPropertiesWithSameOwner <= 1)
                {
                    _context.Owners.Remove(currentOwner);
                }



                existingProperty.Status = PropertyStatus.Available;
                


                await _context.SaveChangesAsync();
                var updatedProperty = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (updatedProperty == null) return NotFound(new { message = $"Property with ID {id} not found after resetting sale status." });
                
                return Ok();
            }

            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while resetting the property sale status. Please try again later. " + ex.Message,
                    statusCode: 500
                    );
            }
        }
        


        [HttpPost]
        [Route("sale/marking-as-process/{id}")]
        [Authorize]
        public async Task<ActionResult> MarkAsProcess(int id, [FromBody] SaleMarkingProcessPropertyDto dto)
        {
            try
            {
             
                var ownerId = dto.OwnerId;


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

                var owner = await _context.Owners.FindAsync(ownerId);

                if (owner == null)
                {
                    return NotFound(new { message = $"Owner with ID {ownerId} not found." });
                }

                existingProperty.Owner = owner;
                existingProperty.Status = PropertyStatus.InProcess;
                await _context.SaveChangesAsync();
                

                return Ok();


            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while marking the property as in process. Please try again later. " + ex.Message,
                    statusCode: 500);
            }
        }


        [HttpPost]
        [Route("sale/{id}")]
        [Authorize]
        public async Task<ActionResult> MarkAsSold(int id)
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
                    .Include(o => o.Owner)
                    .Where(w => w.Id == id)
                    .FirstOrDefaultAsync();

                if (existingProperty == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }

                if(existingProperty.ListingType == ListingType.Rent)
                {
                    return BadRequest(new { message = "Only properties for sale can be marked as sold." });
                }

                if(existingProperty.Status == PropertyStatus.Sold)
                {
                    return BadRequest(new { message = "Property is already marked as sold." });
                }

             

                var HasCreated = _context.Properties.
                    Include(p => p.CreatedBy)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefault(p => p.Id == id);


                var MyUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                if (MyUser == null)
                {
                    return StatusCode(403, new { message = "User not found." });
                }


                bool CanEdit = false;


                if (existingProperty.CreatedBy != null)
                {
                    CanEdit = _userPermission.HasSuperiorRoleTo(Convert.ToInt32(userId), existingProperty.CreatedBy.Id);
                }


                if (!CanEdit)
                    return StatusCode(403, new { message = "You can`t edit this property" });



               if(existingProperty.Owner == null)
                {
                    return BadRequest(new { message = "Property must have an owner before being marked as sold." });
                }


               existingProperty.UpdatedAt = DateTime.UtcNow;
                existingProperty.Status = PropertyStatus.Sold;
                await _context.SaveChangesAsync();
               
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while marking the property as sold. Please try again later. " + ex.Message
                );
            }
        }

        [HttpPost]
        [Authorize]
        [Route("rent/{id}")]
        public async Task<ActionResult> RentProperty(int id, [FromBody] PropertyRentRequestDto dto)
        {

            try
            {

                var existOwner = _context.Owners.Find(dto.OwnerId);
                if (existOwner == null) return NotFound();



                return Ok();
            }
            catch (Exception ex)
            {

                return Problem(detail: "An error occurred: " + ex.Message);
            }


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
