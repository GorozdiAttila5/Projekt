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
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
                return RedirectToAction("Index");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Index");

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorToast"] = "Could not remove old roles.";
                return RedirectToAction("Index");
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                TempData["ErrorToast"] = "Could not add new role.";
            }
            else
            {
                TempData["SuccessToast"] = $"Role updated to {newRole} for {user.UserName}.";
            }

            return RedirectToAction("Index");
        }

        // AJAX endpoint for single role change
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRoleAjax(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
                return Json(new { success = false, message = "Invalid input." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var currentRoles = await _userManager.GetRolesAsync(user);

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return Json(new { success = false, message = "Could not remove old roles." });

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
                return Json(new { success = false, message = "Could not add new role." });

            // return the userId and newRole so client can update UI immediately
            return Json(new { success = true, message = $"Role updated to {newRole} for {user.UserName}.", userId = userId, newRole = newRole });
        }

        // AJAX endpoint for bulk role changes (userIds[] + newRole)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRolesAjax(string[] userIds, string newRole)
        {
            if (userIds == null || userIds.Length == 0 || string.IsNullOrEmpty(newRole))
                return Json(new { success = false, message = "Invalid input." });

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
                return Json(new { success = results.Count == 0, message = $"Completed with errors. Success: {successCount}", errors = results, updatedUserIds = succeeded });
            }

            return Json(new { success = true, message = $"Role '{newRole}' assigned to {successCount} user(s).", updatedUserIds = succeeded });
        }
    }
}