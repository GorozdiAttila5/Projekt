using BugReport.Entities;
using BugReport.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugReport.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController
        (
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
            if (userIds == null || userIds.Length == 0 || string.IsNullOrEmpty(newRole))
            {
                TempData["ErrorToast"] = "Invalid input.";
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
                    results.Add($"User {uid} not found.");
                    continue;
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    results.Add($"Failed to remove roles for {user.UserName}.");
                    continue;
                }

                var addResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!addResult.Succeeded)
                {
                    results.Add($"Failed to add role to {user.UserName}.");
                    continue;
                }

                successCount++;
                succeeded.Add(uid);
            }

            if (results.Any())
            {
                TempData["WarnToast"] =
                    $"Completed with errors.\nSuccess: {successCount}\n" +
                    string.Join("\n", results);

                return Json(new { success = false, reload = true });
            }

            TempData["SuccessToast"] = $"Role '{newRole}' assigned to {successCount} user(s).";

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