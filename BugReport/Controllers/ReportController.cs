using BugReport.Data;
using BugReport.Entities;
using BugReport.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Localization;
using System;
using System.Data;
using System.Security.Cryptography.Xml;

namespace BugReport.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IStringLocalizer<ReportController> _localizer;

        public ReportController
        (
            ApplicationDbContext context,
            UserManager<User> userManager,
            IStringLocalizer<ReportController> localizer
        )
        {
            _context = context;
            _userManager = userManager;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string[]? statuses)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Reports
                .Where(r => r.ReporterId == userId || r.Assignees.Any(a => a.Id == userId || User.IsInRole("Admin")))
                .Include(r => r.Reporter)
                .Include(r => r.ChangeLogs)!.ThenInclude(cl => cl.Status)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(r => r.Title.ToLower().Contains(search));
            }

            var reports = await query
                .OrderByDescending(r =>
                    r.ChangeLogs!
                        .OrderByDescending(cl => cl.Timestamp)
                        .Select(cl => (DateTime?)cl.Timestamp)
                        .FirstOrDefault()
                    ?? r.CreatedAt
                )
                .ToListAsync();

            var list = new List<ReportViewModel>();

            foreach (var report in reports)
            {
                var latestChangeLog = report.ChangeLogs?
                    .OrderByDescending(cl => cl.Timestamp)
                    .FirstOrDefault();

                if (statuses != null && statuses.Length > 0)
                {
                    var latestStatus = latestChangeLog?.Status?.Name;

                    if (latestStatus == null || !statuses.Contains(latestStatus))
                        continue;
                }

                list.Add(new ReportViewModel
                {
                    Report = report,
                    LatestChangeLog = latestChangeLog
                });
            }

            ViewBag.Statuses = await _context.Statuses
                .Select(s => s.Name)
                .ToListAsync();

            ViewBag.SelectedStatuses = statuses ?? Array.Empty<string>();

            return View(list);
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public IActionResult Create() => View();

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            string temp = string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            var reporter = await _userManager.GetUserAsync(User);

            if (!User.IsInRole("Student"))
                return Forbid();

            var assignees = await _context.Users
            .Where(u => model.Assignees.Contains(u.Id))
            .ToListAsync();

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter!.Id,
                Title = model.Title,
                Assignees = assignees,
                Description = model.Description,
                Attachments = new List<Attachment>(),
                ChangeLogs = new List<ChangeLog>()
            };

            if(model.Attachments != null && model.Attachments.Any())
            {
                var uploadPath = Path.Combine("wwwroot", "uploads", "reports", report.Id.ToString());
                Directory.CreateDirectory(uploadPath);

                foreach (var file in model.Attachments)
                {
                    var fileId = Guid.NewGuid();
                    var fileName = fileId + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    report.Attachments.Add(new Attachment
                    {
                        Id = fileId,
                        FileName = fileName,
                        FilePath = filePath
                    });
                }
            }

            var status = await _context.Statuses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.NormalizedName == "INCOMING");

            if (status is null)
            {
                temp = _localizer["status-not-found"];
                TempData["ErrorToast"] = temp;
                return View(model);
            }

            report.ChangeLogs.Add(new ChangeLog
            {
                Id = Guid.NewGuid(),
                StatusId = status.Id,
                UserId = reporter.Id,
                ChangeDescription = $"Created a new report."
            });

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            temp = _localizer["create-success"];
            TempData["SuccessToast"] = temp;
            return RedirectToAction("Index", "Report");
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            var report = _context.Reports
                .Where(r => r.Id == id)
                .Include(r => r.Reporter)
                .Include(r => r.Assignees)
                .Include(r => r.Messages)!.ThenInclude(m => m.User)
                .Include(r => r.Attachments)
                .Include(r => r.ChangeLogs)!.ThenInclude(cl => cl.Status)
                .Include(r => r.ChangeLogs)!.ThenInclude(cl => cl.User)
                .AsNoTracking()
                .Select(r => new ReportViewModel
                {
                    Report = r,
                    LatestChangeLog = r.ChangeLogs!
                        .OrderByDescending(cl => cl.Timestamp)
                        .FirstOrDefault(),
                    ChangeLogs = r.ChangeLogs!
                        .OrderByDescending(cl => cl.Timestamp)
                        .ToList(),
                    Messages = r.Messages!
                        .OrderBy(m => m.TimeStamp)
                        .ToList()
                })
                .FirstOrDefault();

            if (report == null)
                return RedirectToAction("Index");

            ViewBag.Statuses = _context.Statuses
                .OrderBy(s => s.Id)
                .ToList();

            return View(report);
        }

        [Authorize(Roles = "Instructor,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(Guid reportId, Guid statusId)
        {
            string temp = string.Empty;
            var user = await _userManager.GetUserAsync(User);

            var report = await _context.Reports
                .Include(r => r.Assignees)
                .Include(r => r.ChangeLogs)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return NotFound();

            bool isAssignee = report.Assignees.Any(a => a.Id == user!.Id);
            bool isAdmin = await _userManager.IsInRoleAsync(user!, "Admin");

            if (!isAssignee && !isAdmin)
                return Forbid();

            var newStatus = await _context.Statuses
                .FirstOrDefaultAsync(s => s.Id == statusId);

            if (newStatus == null)
                return BadRequest("Invalid status");

            _context.ChangeLogs.Add(new ChangeLog
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                UserId = user!.Id,
                Timestamp = DateTime.UtcNow,
                StatusId = newStatus.Id,
                ChangeDescription = $"Changed status to {newStatus.Name}."
            });

            await _context.SaveChangesAsync();

            temp = _localizer["status-change-success"];
            TempData["SuccessToast"] = temp;

            return RedirectToAction("Details", new { id = report.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Message(AddMessageViewModel model)
        {
            string temp = string.Empty;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var report = await _context.Reports
                .Include(r => r.Messages)
                .Include(r => r.ChangeLogs)
                .FirstOrDefaultAsync(r => r.Id == model.ReportId);

            if (report == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var message = new Message
            {
                ReportId = model.ReportId,
                Text = model.Text,
                TimeStamp = DateTime.UtcNow,
                UserId = user!.Id
            };

            _context.Messages.Add(message);

            var latestStatusId = report.ChangeLogs!
                .OrderByDescending(cl => cl.Timestamp)
                .Select(cl => cl.StatusId)
                .FirstOrDefault();

            var changeLog = new ChangeLog
            {
                Id = Guid.NewGuid(),
                ReportId = report.Id,
                UserId = user.Id,
                StatusId = latestStatusId,
                ChangeDescription = "Added a new message.",
                Timestamp = DateTime.UtcNow
            };

            _context.ChangeLogs.Add(changeLog);

            await _context.SaveChangesAsync();

            temp = _localizer["message-success"];
            TempData["SuccessToast"] = temp;

            // Extract initials
            string initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper();

            return Json(new
            {
                user = $"{user.FirstName} {user.LastName}",
                initials = initials,
                timestamp = message.TimeStamp.ToString("g"),
                text = message.Text,
                changeDescription = changeLog.ChangeDescription
            });
        }

        [Authorize(Roles = "Student")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.Reports
                .Include(r => r.Assignees)
                .Include(r => r.Attachments)
                .Include(r => r.ChangeLogs)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return RedirectToAction("Index");

            bool isReporter = report.ReporterId == userId;
            bool isAssignee = report.Assignees.Any(a => a.Id == userId);

            if (!isReporter && !isAssignee)
                return Forbid();

            var model = new EditReportViewModel
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Assignees = report.Assignees.Select(a => a.Id).ToList(),
                Attachments = report.Attachments!.Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath
                }).ToList(),
                ExistingAttachments = report.Attachments!.Select(a => a.Id).ToList(),
                RowVersion = report.RowVersion
            };

            return View(model);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditReportViewModel model)
        {
            string temp = string.Empty;

            model.NewAttachments ??= new List<IFormFile>();
            model.ExistingAttachments ??= new List<Guid>();

            if (!ModelState.IsValid)
            {
                ViewBag.Statuses = await _context.Statuses.ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var userId = _userManager.GetUserId(User);

            var report = await _context.Reports
                .Include(r => r.Assignees)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (report == null)
                return RedirectToAction("Index");

            if (report.ReporterId != userId)
                return Forbid();

            var createdFiles = new List<string>();

            try
            {
                // Attach RowVersion for concurrency
                _context.Entry(report).OriginalValues["RowVersion"] = model.RowVersion;

                // Mark Report as unchanged so EF doesn't auto-flag everything as modified
                _context.Entry(report).State = EntityState.Unchanged;

                // Apply only actual modified properties
                report.Title = model.Title;
                report.Description = model.Description;

                _context.Entry(report).Property(r => r.Title).IsModified = true;
                _context.Entry(report).Property(r => r.Description).IsModified = true;

                // --- ASSIGNEES ---
                var newIds = model.Assignees;
                var oldIds = report.Assignees.Select(a => a.Id).ToList();

                var toAdd = newIds.Except(oldIds);
                var toRemove = oldIds.Except(newIds);

                foreach (var id in toAdd)
                {
                    var users = await _context.Users.FindAsync(id);
                    if (users != null)
                        report.Assignees.Add(users);
                }

                foreach (var id in toRemove)
                {
                    var users = report.Assignees.First(a => a.Id == id);
                    report.Assignees.Remove(users);
                }

                // Mark only the Assignees navigation as modified
                _context.Entry(report).Collection(r => r.Assignees).IsModified = true;

                // --- FIRST SAVE: only update basic report data ---
                await _context.SaveChangesAsync();



                //
                // 📌 ATTACHMENT OPERATIONS (do NOT affect RowVersion now)
                //

                // --- Remove attachments ---
                var keep = model.ExistingAttachments;
                var toDelete = report.Attachments!.Where(a => !keep.Contains(a.Id)).ToList();

                foreach (var att in toDelete)
                {
                    if (System.IO.File.Exists(att.FilePath))
                        System.IO.File.Delete(att.FilePath);

                    _context.Attachments.Remove(att);
                }

                // --- Add new attachments ---
                if (model.NewAttachments.Any())
                {
                    var uploadPath = Path.Combine("wwwroot", "uploads", "reports", report.Id.ToString());
                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in model.NewAttachments)
                    {
                        var fileId = Guid.NewGuid();
                        var fileName = fileId + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        createdFiles.Add(filePath);

                        _context.Attachments.Add(new Attachment
                        {
                            Id = fileId,
                            ReportId = report.Id,
                            FileName = fileName,
                            FilePath = filePath
                        });
                    }
                }

                var lastStatusId = await _context.ChangeLogs
                    .Where(cl => cl.ReportId == report.Id)
                    .OrderByDescending(cl => cl.Timestamp)
                    .Select(cl => cl.StatusId)
                    .FirstOrDefaultAsync();

                _context.ChangeLogs.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    UserId = userId!,
                    StatusId = lastStatusId,
                    Timestamp = DateTime.UtcNow,
                    ChangeDescription = "Updated the report."
                });


                await _context.SaveChangesAsync();

                temp = _localizer["edit-success"];
                TempData["SuccessToast"] = temp;
                return RedirectToAction("Details", new { id = report.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var f in createdFiles)
                    if (System.IO.File.Exists(f))
                        System.IO.File.Delete(f);

                temp = _localizer["edit-concurrency"];
                TempData["ErrorToast"] = temp;
                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (Exception)
            {
                foreach (var f in createdFiles)
                    if (System.IO.File.Exists(f))
                        System.IO.File.Delete(f);

                ViewBag.Statuses = await _context.Statuses.ToListAsync();
                temp = _localizer["edit-error"];
                TempData["ErrorToast"] = temp;
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            string temp = string.Empty;
            var report = await _context.Reports
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report is null)
                return RedirectToAction("Index");

            var userId = _userManager.GetUserId(User);
            if (report.ReporterId != userId)
                return Forbid();

            if (report.Attachments != null)
            {
                foreach (var attachment in report.Attachments)
                {
                    if (System.IO.File.Exists(attachment.FilePath))
                        System.IO.File.Delete(attachment.FilePath);
                }

                var folder = Path.Combine("wwwroot", "uploads", "reports", report.Id.ToString());
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }

            var messages = _context.Messages.Where(m => m.ReportId == id);
            _context.Messages.RemoveRange(messages);

            var logs = _context.ChangeLogs.Where(cl => cl.ReportId == id);
            _context.ChangeLogs.RemoveRange(logs);

            _context.Reports.Remove(report);

            await _context.SaveChangesAsync();

            temp = _localizer["delete-success"];
            TempData["WarnToast"] = temp;
            return RedirectToAction("Index");
        }
    }
}