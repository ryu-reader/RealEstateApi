using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;
using System.Runtime;
using System.Security.Claims;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly JwtService _jwt;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;


        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger, JwtService jwt)
        {
            _context = context;
            _logger = logger;
            _jwt = jwt;
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            // Obtienes info del token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.Identity?.Name;

            var role = _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.Id.ToString() == userId)
                        .Select(u => u.Role.Name)
                        .FirstOrDefault();

            return Ok(new { userId, username, role });
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null)
                return Unauthorized("Invalid username or password.");

            // Verificar contraseña con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized("Invalid username or password.");

            // Generar tokens
            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            // Guardar refresh token en DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                accessToken,
                refreshToken,
                username = user.Username,
                role = user.Role.Name,
                userId = user.Id
            });
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

                if (dTO.Password != dTO.RepeatPassword)
                {
                    return BadRequest("Passwords do not match.");
                }


                bool HasRoles = await _context.Roles.AnyAsync();


                if (!HasRoles)
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


                if (Role == null)
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

        
        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> Refresh()
        {
            // Leer refresh token desde cookie
            var refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized();

            // Buscar usuario en DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return Unauthorized();

            // Generar nuevos tokens
            var newAccessToken = _jwt.GenerateAccessToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            // Actualizar cookies
            Response.Cookies.Append("access_token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }






    }
}

