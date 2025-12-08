using BugReport.Entities;
using BugReport.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace BugReport.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<AccountController> _localizer;

        public AccountController
        (
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IStringLocalizer<AccountController> localizer
        )
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _localizer = localizer;
        }


        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            string temp = string.Empty;

            if(!ModelState.IsValid)
                return View(model);

            var user = new User
            {
                Email = model.Email,
                UserName = model.UserName,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                temp = _localizer["invalid-register"];
                List<string> errors = new List<string>();

                foreach (var error in result.Errors)
                    errors.Add(error.Description);

                TempData["ErrorToast"] = temp + string.Join('\n', errors);

                return View(model);
            }

            const string ROLE = "Student";
            if (!await _roleManager.RoleExistsAsync(ROLE))
                await _roleManager.CreateAsync(new IdentityRole(ROLE));

            await _userManager.AddToRoleAsync(user, ROLE);

            await _signInManager.SignInAsync(user, isPersistent: true);

            temp = _localizer["successful-register"];
            TempData["SuccessToast"] = temp;

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            string temp = string.Empty;

            if(!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                temp = _localizer["invalid-login"];
                TempData["ErrorToast"] = temp;

                return View(model);
            }

            temp = _localizer["successful-login"];
            TempData["SuccessToast"] = temp;
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(VerifyEmailViewModel model)
        {
            string temp = string.Empty;
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
            {
                temp = _localizer["user-not-found"];
                TempData["ErrorToast"] = temp;

                return View(model);
            }

            temp = _localizer["email-sent"];
            TempData["SuccessToast"] = temp;
            return RedirectToAction("ResetPassword", new { id = user.Id, });
        }

        [HttpGet]
        public IActionResult ResetPassword(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("ForgotPassword");

            return View(new ResetPasswordViewModel { Id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            string temp = string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);

            if(user is null)
            {
                temp = _localizer["user-not-found"];
                TempData["ErrorToast"] = temp;

                return View(model);
            }

            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                temp = _localizer["invalid-password-reset"];
                TempData["ErrorToast"] = temp;

                return View(model);
            }

            result = await _userManager.AddPasswordAsync(user, model.Password);

            temp = _localizer["successful-password-reset"];
            TempData["SuccessToast"] = temp;
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            string temp = string.Empty;
            await _signInManager.SignOutAsync();

            temp = _localizer["successful-logout"];
            TempData["SuccessToast"] = temp;
            return RedirectToAction("Login");
        }
    }
}
