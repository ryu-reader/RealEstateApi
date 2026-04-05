using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public PropertiesController(ILogger<PropertiesController> logger, ApplicationDbContext context, UserPermission userPermission)
        {
            _logger = logger;
            _context = context;
            _userPermission = userPermission;
        }


        [HttpGet]
        public async Task<ActionResult<List<PropertyGet>>> Get(int Page = 1, string Code = "", int Type = -1)
        {
            

            try
            {

                //pagination
                int pageSize = 10;


                var query = _context.Properties.AsQueryable();

                if (!string.IsNullOrEmpty(Code))
                {
                    query = query.Where(p => p.Code.Contains(Code));
                }


                if (Type != -1 && Enum.IsDefined(typeof(PropertyType), Type))
                {
                    query = query.Where(p => p.Type == (PropertyType)Type);
                }


                var properties = await query
                    .Skip((Page - 1) * pageSize)
                    .Take(pageSize)
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
                        Type = p.Type,
                        Image = p.Image,
                        Images = p.Images,
                        Created = p.CreatedBy != null ? p.CreatedBy.Id : 0,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                



                return properties;
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
                var property = await _context.Properties.FindAsync(id);
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
                    Longitude = PropertyWithCreator.Longitude,
                    Type = PropertyWithCreator.Type,
                    Image = PropertyWithCreator.Image,
                    Images = PropertyWithCreator.Images,
                    Created = PropertyWithCreator.CreatedBy != null ? PropertyWithCreator.CreatedBy.Id : 0,
                    CreatedAt = PropertyWithCreator.CreatedAt,
                    UpdatedAt = PropertyWithCreator.UpdatedAt
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

                if(property.Type == null) property.Type = PropertyType.House;

                var CreatedByUser = await _context.Users.FindAsync(Convert.ToInt32(userId));

                var newProperty = new Property
                {
                    Name = property.Name,
                    Code = property.Code,
                    Description = property.Description,
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
                    Type = readProperty.Type,
                    Image = readProperty.Image,
                    Images = readProperty.Images,
                    Created = readProperty.CreatedBy != null ? readProperty.CreatedBy.Id : 0,
                    CreatedAt = readProperty.CreatedAt,
                    UpdatedAt = readProperty.UpdatedAt
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
                        Type = updatedProperty.Type,
                        Image = updatedProperty.Image,
                        Images = updatedProperty.Images,
                        Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                        CreatedAt = updatedProperty.CreatedAt,
                        UpdatedAt = updatedProperty.UpdatedAt
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

                List<string> ImagesList = new List<string>();

                foreach (var image in Images)
                {

                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(image.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";

                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "properties", existingProperty.Id.ToString());

                    if (!Directory.Exists(folderPath))
                    {
                        System.IO.Directory.CreateDirectory(folderPath);
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
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt
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
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt
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
                    Type = updatedProperty.Type,
                    Image = updatedProperty.Image,
                    Images = updatedProperty.Images,
                    Created = updatedProperty.CreatedBy != null ? updatedProperty.CreatedBy.Id : 0,
                    CreatedAt = updatedProperty.CreatedAt,
                    UpdatedAt = updatedProperty.UpdatedAt
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





    }
}
