using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertiesController : ControllerBase
    {

        private readonly ILogger<PropertiesController> _logger;
        private readonly ApplicationDbContext _context;

        public PropertiesController(ILogger<PropertiesController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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
        public async Task<ActionResult<Property>> Create([FromBody] PropertyAddDto property)
        {
            try
            {


                if(property == null)
                {
                    return BadRequest(new { message = "Property data is required." });
                }

                if(property.Type == null) property.Type = PropertyType.House;

                var newProperty = new Property
                {
                    Name = property.Name,
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





    }
}
