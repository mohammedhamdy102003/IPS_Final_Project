using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;
using IPS_PROJECT.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IPS_PROJECT.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<USERS> _userManager;
        private readonly AppDbContext _context;

        public UsersController(UserManager<USERS> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // جيب IDs الناس اللى عندهم pending admin request عشان متظهرش في Users table
            var pendingAdminUserIds = await _context.AdminRequests
                .Where(r => r.Status == "Pending")
                .Select(r => r.UserId)
                .ToListAsync();

            var users = _userManager.Users
                .Where(u => !pendingAdminUserIds.Contains(u.Id))
                .ToList();

            var userDtos = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userDtos.Add(new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "User",
                    Status = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow ? "Inactive" : "Active",
                    CreatedAt = u.CreatedAt != default ? u.CreatedAt : DateTime.Now
                });
            }

            var pendingRequests = await _context.AdminRequests
                .Where(r => r.Status == "Pending")
                .Include(r => r.User)
                .ToListAsync();

            var model = new UsersViewModel
            {
                Users = userDtos,
                PendingAdminRequests = pendingRequests
            };

            return View(model);
        }

        // تغيير الـ Role
        [HttpPost]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            return Ok();
        }

        // Disable / Enable اليوزر
        [HttpPost]
        public async Task<IActionResult> ToggleDisable([FromBody] UserIdDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            bool isDisabled = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow;

            if (isDisabled)
            {
                // Enable - شيل الـ lockout
                await _userManager.SetLockoutEndDateAsync(user, null);
                return Ok(new { status = "Active" });
            }
            else
            {
                // Disable - لغاية سنة 9999
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                // لازم نفعّل الـ lockout feature
                await _userManager.SetLockoutEnabledAsync(user, true);
                return Ok(new { status = "Inactive" });
            }
        }

        // Delete اليوزر
        [HttpPost]
        public async Task<IActionResult> DeleteUser([FromBody] UserIdDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound();

            // نشيل الـ admin requests المرتبطة بيه الأول
            var requests = _context.AdminRequests.Where(r => r.UserId == dto.UserId);
            _context.AdminRequests.RemoveRange(requests);
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Errors.Select(e => e.Description));
        }
    }

    // DTO بسيط للـ UserId
    public class UserIdDto
    {
        public string UserId { get; set; }
    }
}
