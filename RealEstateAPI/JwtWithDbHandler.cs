using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace RealEstateAPI
{
    public class JwtWithDbRequirement : IAuthorizationRequirement { }

    public class JwtWithDbHandler : AuthorizationHandler<JwtWithDbRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtService _jwt;

        public JwtWithDbHandler(ApplicationDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            JwtWithDbRequirement requirement)
        {
            var httpContext = (context.Resource as HttpContext) ?? throw new Exception("HttpContext not found");

            // Leer access token de cookie
            var accessToken = httpContext.Request.Cookies["access_token"];
            if (string.IsNullOrEmpty(accessToken))
            {
                context.Fail();
                return;
            }

            ClaimsPrincipal principal;
            try
            {
                // Validar token (firma + expiración)
                principal = _jwt.GetPrincipalFromExpiredToken(accessToken);
            }
            catch
            {
                context.Fail();
                return;
            }

            // Obtener usuario por claim
            var userId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || string.IsNullOrEmpty(user.RefreshToken))
            {
                context.Fail(); // usuario no existe o no tiene refresh token válido
                return;
            }

            // Opcional: validar IP / User-Agent del refresh token
            // if(user.RefreshTokenIP != httpContext.Connection.RemoteIpAddress.ToString()) { context.Fail(); return; }

            // Todo correcto
            context.Succeed(requirement);
        }
    }
}
