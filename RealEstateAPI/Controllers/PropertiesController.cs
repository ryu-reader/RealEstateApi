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
        public async Task<ActionResult<List<Property>>> Get(int Page = 1, string Code = "", int Type = -1)
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
        public async Task<ActionResult<Property>> GetById(int id)
        {
            try
            {
                var property = await _context.Properties.FindAsync(id);
                if (property == null)
                {
                    return NotFound(new { message = $"Property with ID {id} not found." });
                }
                return property;
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
        public async Task<ActionResult<Property>> Create([FromForm] PropertyAddDto property)
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
                    Type = property.Type
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

                

                return CreatedAtAction(nameof(GetById), new { id = newProperty.Id }, newProperty);


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
        public async Task<ActionResult<Property>> Update(int id, [FromForm] PropertyUpdateDto property)
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



                await _context.SaveChangesAsync();
                return Ok(existingProperty);
            }
            catch (Exception ex)
            {
                return Problem(
                    detail: "An error occurred while updating the property. Please try again later. " + ex.Message,
                    statusCode: 500
                );
            }
        }


        [HttpDelete("{id}")]
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
