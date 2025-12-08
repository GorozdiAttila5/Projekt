using BugReport.Entities;
using BugReport.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BugReport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<AdminController> _localizer;

        public AdminController
        (
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IStringLocalizer<AdminController> localizer
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string[]? roles)
        {
            var currentUserId = _userManager.GetUserId(User);

            var query = _userManager.Users
                .Where(u => u.Id != currentUserId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                    u.UserName!.ToLower().Contains(search) ||
                    (u.FirstName + " " + u.LastName).ToLower().Contains(search)
                );
            }

            var users = await query.ToListAsync();
            var list = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                if (roles != null && roles.Length > 0 && !userRoles.Any(r => roles.Contains(r)))
                    continue;

                list.Add(new UserRoleViewModel
                {
                    User = user,
                    Roles = userRoles
                });
            }

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

            ViewBag.SelectedRoles = roles ?? Array.Empty<string>();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRolesAjax(string[] userIds, string newRole)
        {
            string temp = string.Empty;

            if (userIds == null || userIds.Length == 0 || string.IsNullOrEmpty(newRole))
            {
                temp = _localizer["invalid-attempt"];
                TempData["ErrorToast"] = temp;
                return Json(new { success = false, reload = true });
            }

            var results = new List<string>();
            var succeeded = new List<string>();
            int successCount = 0;

            foreach (var uid in userIds.Distinct())
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user == null)
                {
                    temp = _localizer["user-not-found"];
                    results.Add(temp + '(' + uid + ')');
                    continue;
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    temp = _localizer["failed-to-remove"];
                    results.Add(temp + '(' + user.UserName + ')');
                    continue;
                }

                var addResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!addResult.Succeeded)
                {
                    temp = _localizer["failed-to-add"];
                    results.Add(temp + '(' + user.UserName + ')');
                    continue;
                }

                successCount++;
                succeeded.Add(uid);
            }

            if (results.Any())
            {
                temp = _localizer["complited-with-errors"];
                TempData["WarnToast"] = temp + '\n' + string.Join("\n", results);

                return Json(new { success = false, reload = true });
            }

            temp = _localizer["successfully-assigned"];
            TempData["SuccessToast"] = newRole + temp;

            return Json(new { success = true, reload = true });
        }

        [HttpGet]
        public IActionResult TempDataWarn(string n)
        {
            TempData["WarnToast"] = n;
            return Ok();
        }

        [HttpGet]
        public IActionResult TempDataError(string n)
        {
            TempData["ErrorToast"] = n;
            return Ok();
        }
    }
}