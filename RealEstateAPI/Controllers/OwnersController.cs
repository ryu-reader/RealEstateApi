using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.DTO;
using RealEstateAPI.DTO.Owner;
using RealEstateAPI.Models;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnersController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<OwnersController> _logger;

        public OwnersController(ApplicationDbContext context, ILogger<OwnersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ResponsePagination<Owner>>> GetOwners(int Page = 1, int PageSize = 10)
        {
            try
            {
                var query = _context.Owners.AsQueryable();

                var totalCount = await query.CountAsync();

                var owners = await query
                    .Skip((Page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();


                var response = new ResponsePagination<Owner>
                {
                    Data = owners,
                    CurrentPage = Page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / PageSize),
                    PageSize = PageSize,
                    TotalCount = totalCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving owners.");
                return Problem(
                    detail: "An error occurred while retrieving owners. Please try again later.",
                    statusCode: 500
                );
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Owner>> GetOwner(int id)
        {
            try
            {
                var owner = await _context.Owners.FindAsync(id);
                if (owner == null)
                {
                    return NotFound(new { Message = $"Owner with ID {id} not found." });
                }
                return Ok(owner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving owner with ID {id}.");
                return Problem(
                    detail: $"An error occurred while retrieving owner with ID {id}. Please try again later.",
                    statusCode: 500
                );
            }
        }

        [HttpPost]
        public async Task<ActionResult<Owner>> CreateOwner([FromBody] OwnerDTO dTO)
        {
            try
            {
                var owner = new Owner
                {
                    Name = dTO.Name,
                    LastName = dTO.LastName,
                    Identification = dTO.Identification,
                    Email = dTO.Email,
                    Phone = dTO.Phone,
                    Country = dTO.Country
                };

                _context.Owners.Add(owner);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetOwner), new { id = owner.Id }, owner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a new owner.");
                return Problem(
                    detail: "An error occurred while creating a new owner. Please try again later.",
                    statusCode: 500
                );
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOwner(int id, [FromBody] OwnerDTO dTO)
        {
            try
            {
                var owner = await _context.Owners.FindAsync(id);
                if (owner == null)
                {
                    return NotFound(new { Message = $"Owner with ID {id} not found." });
                }
                owner.Name = dTO.Name;
                owner.LastName = dTO.LastName;
                owner.Identification = dTO.Identification;
                owner.Email = dTO.Email;
                owner.Phone = dTO.Phone;
                owner.Country = dTO.Country;
                _context.Owners.Update(owner);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating owner with ID {id}.");
                return Problem(
                    detail: $"An error occurred while updating owner with ID {id}. Please try again later.",
                    statusCode: 500
                );
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOwner(int id)
        {
            try
            {
                var owner = await _context.Owners.FindAsync(id);
                if (owner == null)
                {
                    return NotFound(new { Message = $"Owner with ID {id} not found." });
                }
                _context.Owners.Remove(owner);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting owner with ID {id}.");
                return Problem(
                    detail: $"An error occurred while deleting owner with ID {id}. Please try again later.",
                    statusCode: 500
                );
            }
        }



        }
}