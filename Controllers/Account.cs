using IPS_PROJECT.Data;
using IPS_PROJECT.Models;
using IPS_PROJECT.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;


namespace IPS_PROJECT.Controllers
{

    public class AccountController : Controller
    {
        private readonly SignInManager<USERS> _signInManager;
        private readonly UserManager<USERS> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;

        public AccountController(UserManager<USERS> userManager, SignInManager<USERS> signInManager, IEmailSender emailSender, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("", "Email is required.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("", "Password is required.");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Role))
            {
                ModelState.AddModelError("", "Role is required.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt");
                return View(model);
            }

            if (!await _userManager.IsInRoleAsync(user, model.Role))
            {
                ModelState.AddModelError("", $"User does not have {model.Role} role");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                if (model.Role == "Admin")
                    return RedirectToAction("Index", "DashBoard");

                return RedirectToAction("Index", "UserDashboard");
            }

            if (result.IsNotAllowed)
            {
                ModelState.AddModelError("", "Please confirm your email before logging in.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match");
                return View(model);
            }

            var user = new USERS
            {
                UserName = model.FullName,
                Email = model.Email,
                FullName = model.FullName,
                RequestedRole = model.RegisterType
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await SendConfirmationEmail(user);
                return RedirectToAction("VerifyEmail");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        private async Task SendConfirmationEmail(USERS user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            if (string.IsNullOrEmpty(user.Email))
                throw new InvalidOperationException("User email is missing.");

            if (callbackUrl == null)
                throw new InvalidOperationException("Callback URL is null.");

            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return RedirectToAction("Login");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                var adminExists = await (
                    from u in _context.Users
                    join ur in _context.UserRoles on u.Id equals ur.UserId
                    join r in _context.Roles on ur.RoleId equals r.Id
                    where r.Name == "Admin"
                    select u
                ).AnyAsync();

                if (!adminExists && user.RequestedRole == "Admin")
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                else if (user.RequestedRole == "Admin")
                {
                    // لا نضيفه كـ User ولا كـ Admin - يفضل pending لحد ما يتوافق عليه
                    var request = new AdminRequest
                    {
                        UserId = user.Id,
                        RequestDate = DateTime.Now,
                        Status = "Pending"
                    };

                    _context.AdminRequests.Add(request);
                    await _context.SaveChangesAsync();

                    await SendAdminRequestEmail(user);

                    return RedirectToAction("AdminRequestPending");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                await _signInManager.SignInAsync(user, false);

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "DashBoard");
                else
                    return RedirectToAction("Index", "UserDashboard");
            }

            return View("Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminRequestPending()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            await SendConfirmationEmail(user);

            TempData["StatusMessage"] = "Verification email resent. Please check your inbox.";

            return RedirectToAction("VerifyEmail");
        }

        private async Task SendAdminRequestEmail(USERS user)
        {
            var firstAdmin = await (
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                join r in _context.Roles on ur.RoleId equals r.Id
                where r.Name == "Admin"
                orderby u.CreatedAt
                select u
            ).FirstOrDefaultAsync();

            if (firstAdmin == null)
                return;

            var token = await _userManager.GenerateUserTokenAsync(
                user,
                "Default",
                "AdminApproval"
            );

            token = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token)
            );

            var approveLink = Url.Action(
                "ApproveAdmin",
                "Account",
                new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            var rejectLink = Url.Action(
                "RejectAdmin",
                "Account",
                new { userId = user.Id },
                protocol: Request.Scheme);

            var message = $@"
                Hello {firstAdmin.FullName},<br><br>
                User <b>{user.FullName}</b> requested Admin access.<br><br>
                <a href='{approveLink}' style='padding:10px;background:green;color:white;text-decoration:none'>Approve</a>
                &nbsp;
                <a href='{rejectLink}' style='padding:10px;background:red;color:white;text-decoration:none'>Reject</a>
            ";

            await _emailSender.SendEmailAsync(
                firstAdmin.Email,
                "Admin Access Request",
                message
            );
        }

        // Approve من الـ Email (بيطلب token للأمان)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ApproveAdmin(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));

            var isValid = await _userManager.VerifyUserTokenAsync(
                user,
                "Default",
                "AdminApproval",
                token
            );

            if (!isValid)
                return Unauthorized();

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                await _userManager.AddToRoleAsync(user, "Admin");

            var request = await _context.AdminRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");

            if (request != null)
            {
                request.Status = "Approved";
                await _context.SaveChangesAsync();
            }

            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
                await _emailSender.SendEmailAsync(user.Email!, "Admin Request Approved", $@"
                    Hello {user.FullName},<br><br>
                    Your request for Admin access has been <b style='color:green'>approved</b>.<br><br>
                    <a href='{loginUrl}'>Go to Login</a>
                ");
            }
            catch { }

            return Content("Admin access granted successfully.");
        }

        // ✅ Approve من الـ Dashboard (من غير token)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ApproveAdminDashboard(string userId)
        {
            if (userId == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            // شيل أي role قديم وضيفه كـ Admin
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            await _userManager.AddToRoleAsync(user, "Admin");

            // امسح الـ pending request من الجدول (مش بس نغير status - لازم يختفي من القائمة)
            var request = await _context.AdminRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");

            if (request != null)
            {
                _context.AdminRequests.Remove(request);
                await _context.SaveChangesAsync();
            }

            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
                await _emailSender.SendEmailAsync(user.Email!, "Admin Request Approved", $@"
                    Hello {user.FullName},<br><br>
                    Your request for Admin access has been <b style='color:green'>approved</b>.<br><br>
                    You can now log in as an Admin.
                ");
            }
            catch { }

            return RedirectToAction("Index", "Users");
        }

        // Reject من الـ Email
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RejectAdmin(string userId)
        {
            if (userId == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var request = await _context.AdminRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");

            if (request != null)
            {
                request.Status = "Rejected";
                await _context.SaveChangesAsync();
            }

            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
                await _emailSender.SendEmailAsync(user.Email!, "Admin Request Rejected", $@"
                    Hello {user.FullName},<br><br>
                    Your request for Admin access has been <b style='color:red'>rejected</b>.<br><br>
                    <a href='{loginUrl}'>Go to Login</a>
                ");
            }
            catch { }

            return Content("Admin request rejected.");
        }

        // ✅ Reject من الـ Dashboard
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RejectAdminDashboard(string userId)
        {
            if (userId == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var userEmail = user.Email;
            var userName = user.FullName;

            // امسح الـ pending request أولاً
            var request = await _context.AdminRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");

            if (request != null)
            {
                _context.AdminRequests.Remove(request);
                await _context.SaveChangesAsync();
            }

            // امسح الـ user خالص (ماعندوش role ولا مكانه في السيستم)
            await _userManager.DeleteAsync(user);

            try
            {
                var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
                await _emailSender.SendEmailAsync(userEmail!, "Admin Request Rejected", $@"
                    Hello {userName},<br><br>
                    Your request for Admin access has been <b style='color:red'>rejected</b>.<br><br>
                    If you'd like to join as a regular user, you can register again.
                ");
            }
            catch { }

            return RedirectToAction("Index", "Users");
        }
    }
}