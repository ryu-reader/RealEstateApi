using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAPI.DTO.Role;
using RealEstateAPI.Models;
using RealEstateAPI.Security;
using System.Security.Claims;

namespace RealEstateAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {


        private readonly ApplicationDbContext _context;
        private readonly ILogger<RolesController> _logger;
        private readonly UserPermission _userPermission;

        public RolesController(ApplicationDbContext context, ILogger<RolesController> logger, UserPermission userPermission)
        {
            _context = context;
            _logger = logger;
            _userPermission = userPermission;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<Role>>> GetRoles()
        {
            try
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.VIEW_ROLES);

                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to view roles." });
                }

                var roles = await _context.Roles
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    Permissions = r.PermissionsRole
                        .Select(pr => pr.Permission.Type)
                        .ToList()
                })
                .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize]
        public async Task<ActionResult<Role>> GetRoleById(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.PermissionsRole)
                        .ThenInclude(pr => pr.Permission)
                    .FirstOrDefaultAsync(r => r.Id == id);


                if (role == null)
                {
                    return NotFound(new { message = "Role not found." });
                }
                return Ok(role);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Role>> CreateRole([FromBody] RoleAddRequestDTO dTO)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.ADD_ROLE);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to add roles." });
                }

                bool roleExists = await _context.Roles.AnyAsync(r => r.Name == dTO.Name);
                if (roleExists)
                {
                    return Conflict(new { message = "A role with the same name already exists." });
                }

                bool validLevel = _userPermission.VerifiedRoleLevel(Convert.ToInt32(userId), dTO.Level);

                if(!validLevel) 
                {
                    return StatusCode(403, new { message = "You cannot create a role with a level higher than your own." });
                }

                var role = new Role
                {
                    Name = dTO.Name,
                    Description = dTO.Description,
                    Level = dTO.Level
                };


                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }



        [HttpPut]
        [Route("{id}")]
        [Authorize]
        public async Task<ActionResult<Role>> UpdateRole(int id, [FromBody] RoleEditRequestDTO dTO)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_ROLE);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit roles." });
                }
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found." });
                }
                bool validLevel = _userPermission.VerifiedRoleLevel(Convert.ToInt32(userId), dTO.Level);
                if (!validLevel)
                {
                    return StatusCode(403, new { message = "You cannot update a role with a level higher than your own." });
                }

                role.Name = dTO.Name;
                role.Description = dTO.Description;
                role.Level = dTO.Level;


                await _context.SaveChangesAsync();
                return Ok(role);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }


        [HttpDelete]
        [Route("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteRole(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.DELETE_ROLE);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to delete roles." });
                }
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { message = "Role not found." });
                }
                bool validLevel = _userPermission.VerifiedRoleLevel(Convert.ToInt32(userId), role.Level);
                if (!validLevel)
                {
                    return StatusCode(403, new { message = "You cannot delete a role with a level higher than your own." });
                }
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }



        [HttpPost]
        [Route("{id}/permissions")]
        [Authorize]
        public async Task<ActionResult> AssignPermissionToRole(int id, [FromBody] List<PermissionType> permissions)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var permission = _userPermission.HasPermission(Convert.ToInt32(userId), PermissionType.EDIT_ROLE);
                if (!permission)
                {
                    return StatusCode(403, new { message = "You do not have permission to edit roles." });
                }


                var role = await _context.Roles
                    .Include(r => r.PermissionsRole)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (role == null)
                {
                    return NotFound(new { message = "Role not found." });
                }

                bool validLevel = _userPermission.VerifiedRoleLevel(Convert.ToInt32(userId), role.Level);
                if (!validLevel)
                {
                    return StatusCode(403, new { message = "You cannot assign permissions to a role with a level higher than your own." });
                }

                // Eliminar permisos existentes
                _context.PermissionRoles.RemoveRange(role.PermissionsRole);


                // Asignar nuevos permisos
                foreach (var perm in permissions)
                {
                    var permissionEntity = await _context.Permissions.FirstOrDefaultAsync(p => p.Type == perm);

                    if(permissionEntity == null) continue;
                    
                    var hasUserPermission = _userPermission.HasPermission(Convert.ToInt32(userId), perm);

                    if (!hasUserPermission) continue;

               
                        _context.PermissionRoles.Add(new PermissionRole
                        {
                            Role = role,
                            Permission = permissionEntity
                        });
                    
                }



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
