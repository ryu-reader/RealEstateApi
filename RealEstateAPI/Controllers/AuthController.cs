using Fido2NetLib;
using Fido2NetLib.Objects;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RealEstateAPI.DTO;
using RealEstateAPI.Models;
using System.Buffers.Text;
using System.Runtime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static Fido2NetLib.AuthenticatorAttestationRawResponse;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly JwtService _jwt;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IFido2 _fido2;


        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger, JwtService jwt, IFido2 fido2)
        {
            _context = context;
            _logger = logger;
            _jwt = jwt;
            _fido2 = fido2;
        }

        private string FormatException(Exception e)
        {
            return string.Format("{0}{1}", e.Message, e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
        }


        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<Models.User>> GetUserInfo()
        {
            // Obtienes info del token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var username = User.Identity?.Name;

            var role = _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.Id.ToString() == userId)
                        .Select(u => u.Role.Name)
                        .FirstOrDefault();

            var MyUser = _context.Users
                        .Include(u => u.Role)
                        .Where(u => u.Id.ToString() == userId)
                        .FirstOrDefault();

            var Permissions = _context.Roles
                        .Include(r => r.PermissionsRole)
                        .Where(r => r.Name == role)
                        .SelectMany(r => r.PermissionsRole.Select(p => p.Permission.Type))
                        .ToList();


            return Ok(new { MyUser, Permissions});
        }



        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null)
                return Unauthorized("Invalid username or email.");

            // Verificar contraseña con BCrypt
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return Unauthorized("Invalid password.");

            // Generar tokens
            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            //Get Permissions
            var Permissions = _context.Roles
                        .Include(r => r.PermissionsRole)
                        .Where(r => r.Name == user.Role.Name)
                        .SelectMany(r => r.PermissionsRole.Select(p => p.Permission.Type))
                        .ToList();


            // Guardar refresh token en DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                accessToken,
                refreshToken,
                User = user,
                Permissions,
                username = user.Username,
                role = user.Role.Name,
                userId = user.Id
            });
        }

        /*
        [HttpPost("register")]
        public async Task<ActionResult<Models.User>> Register([FromForm] UserAddDTO dTO)
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






                Models.User user = new Models.User
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
        */

        [HttpPost("find-user")]
        public async Task<ActionResult<bool>> FindUser([FromBody] FindUserDTO dTO)
        {

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dTO.UsernameOrEmail || u.Email == dTO.UsernameOrEmail);
                if (user != null)
                {
                    return Ok(true);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request: " + ex.Message);
            }


            return NotFound(false);
        }


        [HttpGet("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
            if (user == null)
                return Unauthorized();
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _context.SaveChangesAsync();

            Response.Cookies.Delete("access_token", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.None,
                Secure = true
            });

            Response.Cookies.Delete("refresh_token", new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.None,
                Secure = true
            });


            return Ok("Logged out successfully.");
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
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(1)
            });

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }



        /* FIDO 2 */
        [HttpPost("passkey/register/options")]
        public IActionResult RegisterPasskeyOptions([FromBody] int userId)
        {
            var user = _context.Users.Find(userId);

            var fidoUser = new Fido2User
            {
                DisplayName = user.Name,
                Name = user.Email,
                Id = Encoding.UTF8.GetBytes(user.Id.ToString())
            };

            var existingCredentials = _context.UserPasskeys
                .Where(x => x.UserId == userId.ToString())
                .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
                .ToList();

            var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
            {
                User = fidoUser,
                ExcludeCredentials = existingCredentials,
                AuthenticatorSelection = AuthenticatorSelection.Default,
                Extensions = new AuthenticationExtensionsClientInputs
                {
                    CredProps = true  // Enable credential properties extension
                }
            });

            HttpContext.Session.SetString("fido.attestationOptions", options.ToJson());

            return Ok(options);
        }

        [HttpPost("passkey/register/verify")]
        public async Task<IActionResult> RegisterPasskeyVerify([FromBody] AuthenticatorAttestationRawResponse credential)
        {
            var jsonOptions = HttpContext.Session.GetString("fido.attestationOptions");
            var options = CredentialCreateOptions.FromJson(jsonOptions);

            IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, token) =>
            {
                var credentialIds = _context.UserPasskeys
                    .Select(x => x.CredentialId)
                    .AsEnumerable();

                bool exists = !credentialIds.Any(x => x.SequenceEqual(args.CredentialId));

                return await Task.FromResult(exists);
            };

            var success = await _fido2.MakeNewCredentialAsync(
                new MakeNewCredentialParams
                {
                    AttestationResponse = credential,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = callback
                });

            var userId = int.Parse(Encoding.UTF8.GetString(options.User.Id));

            var passkey = new UserPasskey
            {
                UserId = userId.ToString(),
                UserHandle = options.User.Id,
                CredentialId = success.Id,
                PublicKey = success.PublicKey,
                SignCount = success.SignCount,
                DeviceName = "Passkey Device"
            };

            _context.UserPasskeys.Add(passkey);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost("passkey/login/verify")]
        public async Task<IActionResult> LoginPasskeyVerify([FromBody] JsonElement body)
        {
            var rawJson = body.GetRawText();
            Console.WriteLine(rawJson);

            var clientResponse = System.Text.Json.JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(rawJson);

            if (clientResponse == null)
                return BadRequest("No se pudo leer assertion response.");

            var jsonOptions = HttpContext.Session.GetString("fido.assertionOptions");
            var options = AssertionOptions.FromJson(jsonOptions);

            var storedCredential = _context.UserPasskeys
                .AsEnumerable()
                .FirstOrDefault(x => x.CredentialId.SequenceEqual(clientResponse.RawId));

            if (storedCredential == null)
                return BadRequest("Credencial no encontrada.");

            IsUserHandleOwnerOfCredentialIdAsync callback = async (args, token) =>
            {
                bool isOwner = _context.UserPasskeys
                    .AsEnumerable()
                    .Any(x =>
                        x.UserHandle.SequenceEqual(args.UserHandle) &&
                        x.CredentialId.SequenceEqual(args.CredentialId));

                return await Task.FromResult(isOwner);
            };

            var result = await _fido2.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = clientResponse,
                    OriginalOptions = options,
                    StoredPublicKey = storedCredential.PublicKey,
                    StoredSignatureCounter = storedCredential.SignCount,
                    IsUserHandleOwnerOfCredentialIdCallback = callback
                });

            storedCredential.SignCount = result.SignCount;
            _context.UserPasskeys.Update(storedCredential);
            await _context.SaveChangesAsync();

            int IdUser = int.Parse(Encoding.UTF8.GetString(storedCredential.UserHandle));

            Console.WriteLine("User ID from UserHandle: " + IdUser);

            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Id == IdUser);
             if (user == null)
                return BadRequest("Usuario no encontrado.");

            // Generar tokens
            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            //Get Permissions

            var roleName = user.Role?.Name;

            var permissions = _context.Roles
                .Include(r => r.PermissionsRole)
                    .ThenInclude(pr => pr.Permission)
                .Where(r => r.Name == roleName)
                .SelectMany(r => r.PermissionsRole.Select(p => p.Permission.Type))
                .ToList();


            // Guardar refresh token en DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,

                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                accessToken,
                refreshToken,
                User = user,
                Permissions = permissions,
                username = user.Username,
                role = user.Role.Name,
                userId = user.Id
            });
        }


        [HttpPost("passkey/login/options")]
        public IActionResult LoginPasskeyOptions([FromBody] FindUserDTO findUser)
        {

            var user = _context.Users.FirstOrDefault(x => x.Username == findUser.UsernameOrEmail || x.Email == findUser.UsernameOrEmail);

            if (user == null)
                return BadRequest("Usuario no encontrado.");

            var userId = user.Id;


            var credentials = _context.UserPasskeys
                .Where(x => x.UserId == userId.ToString())
                .Select(x => new PublicKeyCredentialDescriptor(x.CredentialId))
                .ToList();

            if (!credentials.Any())
                return BadRequest("Usuario no tiene passkeys registradas");


            var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = credentials,
                UserVerification = UserVerificationRequirement.Preferred,
                Extensions = new AuthenticationExtensionsClientInputs
                {
                    Extensions = true
                }
            });

            HttpContext.Session.SetString("fido.assertionOptions", options.ToJson());

            return Ok(options);
        }

        // Endpoint para login con passkey sin necesidad de enviar userId

        [HttpPost("passkey/login/device/options")]
        public IActionResult LoginDevicePasskeyOptions()
        {
            var options = _fido2.GetAssertionOptions(
                new GetAssertionOptionsParams
                {
                    AllowedCredentials = new List<PublicKeyCredentialDescriptor>(),
                    UserVerification = UserVerificationRequirement.Preferred
                });

            HttpContext.Session.SetString("fido.assertionOptions", options.ToJson());

            return Ok(options);
        }

        [HttpPost("passkey/login/device/verify")]
        public async Task<IActionResult> LoginDevicePasskeyVerify([FromBody] JsonElement body)
        {
            var rawJson = body.GetRawText();

            var clientResponse = System.Text.Json.JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(rawJson);

            if (clientResponse == null)
                return BadRequest();

            var jsonOptions = HttpContext.Session.GetString("fido.assertionOptions");
            var options = AssertionOptions.FromJson(jsonOptions);

            var storedCredential = _context.UserPasskeys
                .AsEnumerable()
                .FirstOrDefault(x => x.CredentialId.SequenceEqual(clientResponse.RawId));

            if (storedCredential == null)
                return BadRequest("Credencial no encontrada.");

            IsUserHandleOwnerOfCredentialIdAsync callback = async (args, token) =>
            {
                bool isOwner = _context.UserPasskeys
                    .AsEnumerable()
                    .Any(x =>
                        x.UserHandle.SequenceEqual(args.UserHandle) &&
                        x.CredentialId.SequenceEqual(args.CredentialId));

                return await Task.FromResult(isOwner);
            };

            var result = await _fido2.MakeAssertionAsync(
                new MakeAssertionParams
                {
                    AssertionResponse = clientResponse,
                    OriginalOptions = options,
                    StoredPublicKey = storedCredential.PublicKey,
                    StoredSignatureCounter = storedCredential.SignCount,
                    IsUserHandleOwnerOfCredentialIdCallback = callback
                });

            storedCredential.SignCount = result.SignCount;
            _context.UserPasskeys.Update(storedCredential);
            await _context.SaveChangesAsync();

            var userId = int.Parse(Encoding.UTF8.GetString(storedCredential.UserHandle));

            var user = _context.Users
                .Include(x => x.Role)
                .FirstOrDefault(x => x.Id == userId);


            // Generar tokens
            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();

            //Get Permissions

            var roleName = user.Role?.Name;

            var permissions = _context.Roles
                .Include(r => r.PermissionsRole)
                    .ThenInclude(pr => pr.Permission)
                .Where(r => r.Name == roleName)
                .SelectMany(r => r.PermissionsRole.Select(p => p.Permission.Type))
                .ToList();


            // Guardar refresh token en DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,

                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                accessToken,
                refreshToken,
                User = user,
                Permissions = permissions,
                username = user.Username,
                role = user.Role.Name,
                userId = user.Id
            });




        }

        //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet("passkey/health-check/options")]
        [Authorize]
        public IActionResult HealthCheckOptions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var credentials = _context.UserPasskeys
                .Where(x => x.UserId == userId)
                .Select(x => Base64Url.EncodeToString(x.CredentialId))
                .ToList();

            return Ok(new
            {
                userId = Base64Url.EncodeToString(System.Text.Encoding.UTF8.GetBytes(userId)),
                credentialIds = credentials
            });
        }


    }
}

