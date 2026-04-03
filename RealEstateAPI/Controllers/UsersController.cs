using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;


        public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromForm] UserAddDTO dTO)
        {

            try
            {

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dTO.Email || u.Username == dTO.Username);

                if (existingUser != null)
                {
                    return BadRequest("A user with the same email or username already exists.");
                }

                if(dTO.Password != dTO.RepeatPassword)
                {
                    return BadRequest("Passwords do not match.");
                }


                bool HasRoles = await _context.Roles.AnyAsync();


                _logger.LogError("Status HasRoles: " + (!HasRoles).ToString());

                if(!HasRoles)
                {
                    
                    Role role = new Role
                    {
                        Name = "User",
                        Description = "Default role for new users",
                        Level = 1
                    };

                    _context.Roles.Add(role);
                    _context.SaveChanges();
                }

                var Role = await _context.Roles.FirstOrDefaultAsync(r => r.Level == 1);


                if(Role == null)
                {
                    _logger.LogError("Default role not found in the database.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request: Default Role");
                }


                



                User user = new User
                {
                    Name = dTO.Name,
                    Email = dTO.Email,
                    Username = dTO.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(dTO.Password, 10),
                    Role = Role
                };

                if (dTO.Image != null && dTO.Image.Length > 0)
                {
                    // Obtener extensión del archivo original
                    var extension = Path.GetExtension(dTO.Image.FileName);

                    // Generar un nombre único usando GUID
                    var randomFileName = $"{Guid.NewGuid()}{extension}";

                    // Ruta final
                    var filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "images",
                        "users",
                        randomFileName
                    );

                    // Guardar el archivo
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dTO.Image.CopyToAsync(stream);
                    }

                    // Guardar el nombre en la base de datos
                    user.Image = randomFileName;
                }

                _context.Users.Add(user);
                _context.SaveChanges();


                return Ok(user);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request: " + ex.Message);

            }
        }


    }
}

