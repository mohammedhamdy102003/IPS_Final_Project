 
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
     
    public class Account : Controller
    {
        private readonly SignInManager<USERS> _signInManager;
        private readonly UserManager<USERS> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;

        public Account(UserManager<USERS> userManager, SignInManager<USERS> signInManager, IEmailSender emailSender, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _context = context;
        }

        // GET: /Account/Login

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }



        // POST: /Account/Login

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

            // تحقق من الدور
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




        
       //////////////////////////////// GET: /Account/Register  //////////////////////////////////////////////
       
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }


        //////////////////////////////  POST: /Account/Register   /////////////////////////////////////////////////
        

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



        ////////////////////////////////  Send_Confirmation_Email  //////////////////////////////////////////////
        
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
            {
                throw new InvalidOperationException("User email is missing, cannot send confirmation email.");
            }

            if (callbackUrl == null)
            {
                throw new InvalidOperationException("Callback URL is null, cannot send email.");
            }


            await _emailSender.SendEmailAsync(
                user.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
        }


        //////////////////////////////// Confirm_Email  //////////////////////////////////////////////

        // صفحة استقبال التفعيل (مسموحة للجميع)
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
                    // أول Admin في النظام
                    await _userManager.AddToRoleAsync(user, "Admin");
                }

                else if (user.RequestedRole == "Admin")
                {
                    // إرسال طلب للأدمن
                    await _userManager.AddToRoleAsync(user, "User");

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


        //////////////////////////////// AdminRequestPending  //////////////////////////////////////////////
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AdminRequestPending()
        {
            return View();
        }


        //////////////////////////////// Verify_Email  //////////////////////////////////////////////

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail()
        {
            return View();
        }


        //////////////////////////////// Resend_Confirmation_Email  ////////////////////////////////////////////// 

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ResendConfirmationEmail()
        {
            // جلب اليوزر الحالي
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            
           
            // ارسال ايميل التفعيل
            await SendConfirmationEmail(user);

            TempData["StatusMessage"] = "Verification email resent. Please check your inbox.";

            return RedirectToAction("VerifyEmail");
        }

        //ADMIN REQUEST

        //////////////////////////////// SendAdminRequestEmail  //////////////////////////////////////////////
        private async Task SendAdminRequestEmail(USERS user)
        {
            // 1️⃣ الحصول على أول Admin حسب تاريخ التسجيل
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

            // 2️⃣ إنشاء Token آمن
            var token = await _userManager.GenerateUserTokenAsync(
                user,
                "Default",
                "AdminApproval"
            );

            token = WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(token)
            );

            // 3️⃣ إنشاء رابط الموافقة
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

            // 4️⃣ محتوى الإيميل
            var message = $@"
                   Hello {firstAdmin.FullName},<br><br>

                   User <b>{user.FullName}</b> requested Admin access.<br><br>

                   <a href='{approveLink}' 
                     style='padding:10px;background:green;color:white;text-decoration:none'>
                     Approve
                     </a>

                     &nbsp;

                     <a href='{rejectLink}' 
                     style='padding:10px;background:red;color:white;text-decoration:none'>
                     Reject
                     </a>
                     ";

            // 5️⃣ إرسال الإيميل
            await _emailSender.SendEmailAsync(
                firstAdmin.Email,
                "Admin Access Request",
                message
            );
        }





        //////////////////////////////// Approve_Admin  //////////////////////////////////////////////

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

            // إضافة رول Admin مرة واحدة فقط
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            var request = await _context.AdminRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");

            if (request != null)
            {
                request.Status = "Approved";
                await _context.SaveChangesAsync();
            }

            var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);

            // ✉️ Email مباشرة عبر service
            var subject = "Admin Request Approved";
            var message = $@"
        Hello {user.FullName},<br><br>

        Your request for Admin access has been <b style='color:green'>approved</b>.<br><br>

        You can now log in as Admin from the login page.<br><br>

        <a href='{loginUrl}'>Go to Login</a>
    ";

            await _emailSender.SendEmailAsync(user.Email, subject, message);

            return Content("Admin access granted successfully.");
        }


        //////////////////////////////// Reject_Admin  ////////////////////////////////////////////// 


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

            var loginUrl = Url.Action("Login", "Account", null, Request.Scheme);
            // ✉️ Email مباشرة عبر service
            var subject = "Admin Request Rejected";
            var message = $@"
        Hello {user.FullName},<br><br>

        Your request for Admin access has been <b style='color:red'>rejected</b>.<br><br>

        You can continue using the system as a normal user.

        <a href='{loginUrl}'>Go to Login</a>
    ";

            await _emailSender.SendEmailAsync(user.Email, subject, message);

            return Content("Admin request rejected.");
        }

    }
}
