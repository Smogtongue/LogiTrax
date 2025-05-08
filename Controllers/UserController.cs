using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LogiTrax.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace LogiTrax.Controllers
{
    [Authorize(Roles = "Manager")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        
        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        // GET: /api/users/registered?search=alice&page=1&pageSize=10
        [HttpGet("registered")]
        public async Task<IActionResult> GetRegisteredUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Start with the full queryable list of users.
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                // Perform a case-insensitive match, ensuring no null is passed to Contains.
                query = query.Where(u => 
                    (u.UserName ?? "").Contains(search) ||
                    (u.Email ?? "").Contains(search));
            }
            
            // Use AsNoTracking for performance if the entities won't be modified.
            query = query.AsNoTracking();
            
            // Retrieve the total count for pagination metadata.
            var totalCount = await query.CountAsync();

            // Apply sorting and pagination.
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new 
                {
                    u.Id,
                    u.UserName,
                    u.Email
                })
                .ToListAsync();

            // Return the paginated results along with metadata.
            return Ok(new 
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Users = users
            });
        }
    }
}
