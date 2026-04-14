using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeaturesController : ControllerBase
    {


        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeaturesController> _logger;

        public FeaturesController(ApplicationDbContext context, ILogger<FeaturesController> logger)
        {
            _context = context;
            _logger = logger;
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
        public async Task<ActionResult<Feature>> PostFeature([FromForm] FeatureAddDto featureDto)
        {
            try
            {

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




        }
}
