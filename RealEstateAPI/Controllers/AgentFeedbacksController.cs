using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentFeedbacksController : ControllerBase
    {


        private readonly ApplicationDbContext _context;
        private readonly ILogger<AgentFeedbacksController> _logger;

        public AgentFeedbacksController(ApplicationDbContext context, ILogger<AgentFeedbacksController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpGet]
        [Route("property/{id}")]
        public async Task<ActionResult<IEnumerable<AgentFeedbackDTOResponse>>> GetAllForProperty(int id)
        {
            try
            {

                var property = await _context.Properties
                    .Include(p => p.CreatedBy)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (property == null)
                {
                    return NotFound("Property not found");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == property.CreatedBy.Id);

                if(user == null)
                {
                    return NotFound("User not found");
                }

                var feedbacks = await _context.AgentFeedbacks
                    .Include(f => f.User)
                    .Where(f => f.User.Id == user.Id)
                    .Select(f => new AgentFeedbackDTOResponse
                    {
                        Id = f.Id,
                        UserId = f.User.Id,
                        Feedback = f.Feedback,
                        UserName = f.UserName,
                        UserEmail = f.UserEmail,
                        IpAddress = f.IpAddress,
                        StarRating = f.StarRating,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .ToListAsync();


                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving agent feedbacks");
                return Problem(ex.Message);
            }
        }


        [HttpPost]
        public async Task<ActionResult<AgentFeedbackDTOResponse>> Add([FromForm] AgentFeedbackDTO agentFeedbackDTO)
        {
            try
            {

                var User = await _context.Users.FirstOrDefaultAsync(u => u.Id == agentFeedbackDTO.UserId);

                if (User == null)
                {
                    return NotFound("User not found");
                }

                var IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (string.IsNullOrEmpty(IpAddress))
                {
                    return BadRequest("Unable to determine IP address");
                }

                var existingFeedback =  _context.AgentFeedbacks.FirstOrDefault(f => 
                    f.User.Id == agentFeedbackDTO.UserId && f.IpAddress == IpAddress
                );

                if(existingFeedback != null)
                {
                    return BadRequest("Feedback already exists from this user and IP address");
                }

                if(agentFeedbackDTO.StarRating < 0 || agentFeedbackDTO.StarRating > 5)
                {
                    return BadRequest("Star rating must be between 0 and 5");
                }


                var agentFeedback = new AgentFeedback
                {
                    User = User,
                    Feedback = agentFeedbackDTO.Feedback,
                    UserName = agentFeedbackDTO.UserName,
                    UserEmail = agentFeedbackDTO.UserEmail,
                    IpAddress = IpAddress,
                    StarRating = agentFeedbackDTO.StarRating
                };

                _context.AgentFeedbacks.Add(agentFeedback);

                await _context.SaveChangesAsync();

                var agentFeedbackDTOResponse = new AgentFeedbackDTOResponse
                {
                    Id = agentFeedback.Id,
                    UserId = agentFeedback.User.Id,
                    Feedback = agentFeedback.Feedback,
                    UserName = agentFeedback.UserName,
                    UserEmail = agentFeedback.UserEmail,
                    IpAddress = agentFeedback.IpAddress,
                    StarRating = agentFeedback.StarRating,
                    CreatedAt = agentFeedback.CreatedAt,
                    UpdatedAt = agentFeedback.UpdatedAt
                };



                return CreatedAtAction(nameof(Add), new { id = agentFeedbackDTOResponse.Id }, agentFeedbackDTOResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding agent feedback");
                return Problem(ex.Message);
            }
        }

        [HttpPut]
        [Route("{id}")]
        public async Task<ActionResult<AgentFeedback>> Update(int id, [FromForm] AgentFeedbackDTO agentFeedbackDTO)
        {
            try
            {

                var IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (string.IsNullOrEmpty(IpAddress))
                {
                    return BadRequest("Unable to determine IP address");
                }

                var existingFeedback = _context.AgentFeedbacks.FirstOrDefault(f => f.Id == id && f.IpAddress == IpAddress);
                if (existingFeedback == null)
                {
                    return NotFound("Feedback not found");
                }

                if(agentFeedbackDTO.StarRating < 0 || agentFeedbackDTO.StarRating > 5)
                {
                    return BadRequest("Star rating must be between 0 and 5");
                }

                existingFeedback.Feedback = agentFeedbackDTO.Feedback;
                existingFeedback.UserName = agentFeedbackDTO.UserName;
                existingFeedback.UserEmail = agentFeedbackDTO.UserEmail;
                existingFeedback.StarRating = agentFeedbackDTO.StarRating;
                existingFeedback.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(existingFeedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent feedback");
                return Problem(ex.Message);
            }
        }

        [HttpDelete]
        [Route("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(IpAddress))
                {
                    return BadRequest("Unable to determine IP address");
                }
                var existingFeedback = _context.AgentFeedbacks.FirstOrDefault(f => f.Id == id && f.IpAddress == IpAddress);
                if (existingFeedback == null)
                {
                    return NotFound("Feedback not found");
                }
                _context.AgentFeedbacks.Remove(existingFeedback);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting agent feedback");
                return Problem(ex.Message);
            }
        }



        }
}
