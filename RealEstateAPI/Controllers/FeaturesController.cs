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
    public class FeaturesController : ControllerBase
    {


        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeaturesController> _logger;
        private readonly UserPermission _userPermission;

        public FeaturesController(ApplicationDbContext context, ILogger<FeaturesController> logger, UserPermission userPermission)
        {
            _context = context;
            _logger = logger;
            _userPermission = userPermission;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Feature>>> GetFeatures()
        {
            try
            {
                return await _context.Features.ToListAsync();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Feature>> GetFeature(int id)
        {
            try
            {
                var feature = await _context.Features.FindAsync(id);
                if (feature == null)
                {
                    return NotFound();
                }
                return feature;
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Feature>> PostFeature([FromForm] FeatureAddDto featureDto)
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if(!_userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.ADD_FEATURE))
                {
                    return Forbid("You do not have permission to create a feature.");
                }


                var existingFeature = await _context.Features.FirstOrDefaultAsync(f => f.Name == featureDto.Name);

                if (existingFeature != null)
                {
                    return Conflict($"A feature with the name '{featureDto.Name}' already exists.");
                }


                var feature = new Feature
                {
                    Name = featureDto.Name,
                    Description = featureDto.Description
                };


                if(featureDto.Icon != null)
                {

                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(featureDto.Icon.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";

                    var filePath = Path.Combine(
                       Directory.GetCurrentDirectory(),
                       "wwwroot",
                       "images",
                       "features",
                       randomFileName
                   );

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await featureDto.Icon.CopyToAsync(stream);
                    }

                    feature.Icon = randomFileName;

                }


                _context.Features.Add(feature);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetFeature), new { id = feature.Id }, feature);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutFeature(int id, [FromForm] FeatureUpdateDto featureDto)
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if(!_userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_FEATURE))
                {
                    return Forbid("You do not have permission to edit a feature.");
                }

                var feature = await _context.Features.FindAsync(id);
                if (feature == null)
                {
                    return NotFound();
                }
                feature.Name = featureDto.Name;
                feature.Description = featureDto.Description;
                if (featureDto.Icon != null)
                {
                    if (!string.IsNullOrEmpty(feature.Icon))
                    {
                        var oldFilePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "images",
                            "features",
                            feature.Icon
                        );
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    var extension = Path.GetExtension(featureDto.Icon.FileName);
                    var randomFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(
                       Directory.GetCurrentDirectory(),
                       "wwwroot",
                       "images",
                       "features",
                       randomFileName
                   );
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await featureDto.Icon.CopyToAsync(stream);
                    }
                    feature.Icon = randomFileName;
                }
                _context.Entry(feature).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeature(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if(!_userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.DELETE_FEATURE))
                {
                    return Forbid("You do not have permission to delete a feature.");
                }

                var feature = await _context.Features.FindAsync(id);
                if (feature == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(feature.Icon))
                {
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "features",
                        feature.Icon
                    );

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                //Delete From Table Many To Many
                var propertyFeatures = _context.PropertyFeatures.Where(pf => pf.Feature.Id == id);
                _context.PropertyFeatures.RemoveRange(propertyFeatures);



                _context.Features.Remove(feature);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet("view-image/{id}")]
        public async Task<IActionResult> ViewImage(int id)
        {
            try
            {
                var feature = await _context.Features.FindAsync(id);
                if (feature == null || string.IsNullOrEmpty(feature.Icon))
                {
                    return NotFound();
                }
                var filePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "features",
                    feature.Icon
                );
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound();
                }
                var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(filePath);
                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        private string GetContentType(string filePath)
        {
            // Simple mapping based on file extension
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream",
            };
        }
    }
}
