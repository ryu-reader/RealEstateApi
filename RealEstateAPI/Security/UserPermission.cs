using Microsoft.EntityFrameworkCore;
using RealEstateAPI.Models;

namespace RealEstateAPI.Security
{
    public class UserPermission
    {

        private readonly ApplicationDbContext _context;

        public UserPermission(ApplicationDbContext context)
        {
            _context = context;
        }


        public bool HasPermission(int userId, PermissionType permissionType)
        {
            var user = _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Id == userId);

            if(user == null) return false;

            var Role = _context.Roles.Find(user.Role.Id);

            if(Role == null) return false;

            return _context.PermissionRoles
                    .Include(pr => pr.Permission)
                    .Include(pr => pr.Role)
                    .Where(pr => pr.Role.Id == Role.Id && pr.Permission.Type == permissionType)
                    .Any();
        }

        public bool VerifiedRoleLevel(int userId, int requiredRoleLevel)
        {
            var user = _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Id == userId);
            if (user == null) return false;
            var Role = _context.Roles.Find(user.Role.Id);
            if (Role == null) return false;
            return Role.Level >= requiredRoleLevel;
        }

        //
        public bool HasSuperiorRoleTo(int  userId, int userToId)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Id == userId);

            var userTo = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Id == userToId);

            if (user == null) return false;
            if(userTo == null) return true;


            return user.Role.Level >= userTo.Role.Level;


        }


    }
}
