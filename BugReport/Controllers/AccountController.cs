using BugReport.Entities;
using BugReport.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                string errors = "";

                foreach (var error in result.Errors)
                    errors += error.Description + "\n";

                TempData["ErrorToast"] = errors;

                return View(model);
            }

            const string ROLE = "Student";
            if (!await _roleManager.RoleExistsAsync(ROLE))
                await _roleManager.CreateAsync(new IdentityRole(ROLE));

            await _userManager.AddToRoleAsync(user, ROLE);

            await _signInManager.SignInAsync(user, isPersistent: true);

            TempData["SuccessToast"] = "User registerd successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                TempData["ErrorToast"] = _localizer["invalid-login"];

                return View(model);
            }

            TempData["SuccessToast"] = "User logged in successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(VerifyEmailViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user is null)
            {
                TempData["ErrorToast"] = "User not found!";

                return View(model);
            }

            TempData["SuccessToast"] = $"Email has been sent.";
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
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);

            if(user is null)
            {
                TempData["ErrorToast"] = "User not found!";
                return View(model);
            }

            var result = await _userManager.RemovePasswordAsync(user);
            if (!result.Succeeded)
            {
                string errors = "";

                foreach (var error in result.Errors)
                    errors += error.Description + "\n";

                TempData["ErrorToast"] = errors;

                return View(model);
            }

            result = await _userManager.AddPasswordAsync(user, model.Password);

            TempData["SuccessToast"] = "Password reset successfully.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            TempData["SuccessToast"] = "User logged out successfully.";
            return RedirectToAction("Login");
        }
    }
}
