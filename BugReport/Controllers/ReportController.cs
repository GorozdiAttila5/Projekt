using BugReport.Data;
using BugReport.Entities;
using BugReport.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BugReport.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReportController
        (
            ApplicationDbContext context,
            UserManager<User> userManager
        )
        {
            _context = context;
            _userManager = userManager;
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

            // ---------------------------
            // SEARCH FILTER
            // ---------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(r => r.Title.ToLower().Contains(search));
            }

            // ---------------------------
            // ORDER BY LAST UPDATE
            // ---------------------------
            var reports = await query
                .OrderByDescending(r =>
                    r.ChangeLogs!
                        .OrderByDescending(cl => cl.Timestamp)
                        .Select(cl => (DateTime?)cl.Timestamp)
                        .FirstOrDefault()
                    ?? r.CreatedAt
                )
                .ToListAsync();

            // ---------------------------
            // BUILD VIEW MODEL LIST
            // ---------------------------
            var list = new List<ReportViewModel>();

            foreach (var report in reports)
            {
                var latestChangeLog = report.ChangeLogs?
                    .OrderByDescending(cl => cl.Timestamp)
                    .FirstOrDefault();

                // STATUS FILTER
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
            if (!ModelState.IsValid)
                return View(model);

            var reporter = await _userManager.GetUserAsync(User);

            if(reporter is null)
            {
                TempData["ErrorToast"] = "You must be logged in";
                return View(model);
            }

            var assignees = await _context.Users
            .Where(u => model.Assignees.Contains(u.Id))
            .ToListAsync();

            if (!assignees.Any())
            {
                TempData["ErrorToast"] = "Please select at least one valid assignee";
                return View(model);
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = reporter.Id,
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
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    report.Attachments.Add(new Attachment
                    {
                        Id = Guid.NewGuid(),
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
                TempData["ErrorToast"] = "Default status not found";
                return View(model);
            }

            report.ChangeLogs.Add(new ChangeLog
            {
                Id = Guid.NewGuid(),
                StatusId = status.Id,
                UserId = reporter.Id,
                ChangeDescription = "Report created"
            });

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessToast"] = "Report created successfully";
            return RedirectToAction("Index", "Report");
        }

        [HttpGet]
        public IActionResult Details(Guid id)
        {
            var report = _context.Reports
                .Where(r => r.Id == id)
                .Include(r => r.Reporter)
                .Include(r => r.Assignees)
                .Include(r => r.Messages)!
                    .ThenInclude(m => m.User)
                .Include(r => r.Attachments)
                .Include(r => r.ChangeLogs)!
                    .ThenInclude(cl => cl.Status)
                .Include(r => r.ChangeLogs)!
                    .ThenInclude(cl => cl.User)
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


            if (report is null)
                return RedirectToAction("Index");

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Message(AddMessageViewModel model)
        {
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
                ChangeDescription = "New message added",
                Timestamp = DateTime.UtcNow
            };

            _context.ChangeLogs.Add(changeLog);

            await _context.SaveChangesAsync();

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

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = _userManager.GetUserId(User);

            var report = await _context.Reports
                .Include(r => r.Assignees)
                .Include(r => r.Attachments)
                .Include(r => r.ChangeLogs)!
                    .ThenInclude(cl => cl.Status)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null) return RedirectToAction("Index");

            bool isReporter = report.ReporterId == userId;
            bool isAssignee = report.Assignees.Any(a => a.Id == userId);
            if (!isReporter && !isAssignee) return Forbid();

            var model = new EditReportViewModel
            {
                Id = report.Id,
                Title = report.Title,
                Description = report.Description,
                Assignees = report.Assignees.Select(a => a.Id).ToList(),
                Attachments = report.Attachments.Select(a => new AttachmentViewModel
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath
                }).ToList(),
                ExistingAttachments = report.Attachments.Select(a => a.Id).ToList(),
                StatusId = report.ChangeLogs!
                                 .OrderByDescending(cl => cl.Timestamp)
                                 .FirstOrDefault()!.StatusId
            };

            ViewBag.IsReporter = isReporter;
            ViewBag.Statuses = await _context.Statuses.ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Statuses = await _context.Statuses.ToListAsync();
                ViewBag.IsReporter = true;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            // Load only what EF needs to track properly
            var report = await _context.Reports
                .AsTracking()
                .Include(r => r.Assignees)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (report == null)
                return NotFound();

            bool isReporter = report.ReporterId == userId;
            bool isAssignee = report.Assignees.Any(a => a.Id == userId);
            if (!isReporter && !isAssignee)
                return Forbid();

            // Load last status (no ChangeLogs tracking)
            var lastStatusId = await _context.ChangeLogs
                .Where(cl => cl.ReportId == model.Id)
                .OrderByDescending(cl => cl.Timestamp)
                .Select(cl => cl.StatusId)
                .FirstOrDefaultAsync();

            bool changed = false;

            // --------------------------
            // REPORTER CAN EDIT EVERYTHING
            // --------------------------
            if (isReporter)
            {
                // Title / Description changes
                if (report.Title != model.Title || report.Description != model.Description)
                {
                    report.Title = model.Title;
                    report.Description = model.Description;
                    changed = true;
                }

                // --------------------------
                // UPDATE ASSIGNEES
                // --------------------------
                var removeAssignees = report.Assignees
                    .Where(a => !model.Assignees.Contains(a.Id))
                    .ToList();

                if (removeAssignees.Any())
                {
                    foreach (var a in removeAssignees)
                        report.Assignees.Remove(a);

                    changed = true;
                }

                var addIds = model.Assignees
                    .Except(report.Assignees.Select(a => a.Id))
                    .ToList();

                if (addIds.Any())
                {
                    var addUsers = await _context.Users
                        .Where(u => addIds.Contains(u.Id))
                        .ToListAsync();

                    foreach (var u in addUsers)
                        report.Assignees.Add(u);

                    changed = true;
                }

                // --------------------------
                // REMOVE ATTACHMENTS
                // --------------------------
                var removeAttachments = report.Attachments
                    .Where(a => !model.ExistingAttachments.Contains(a.Id))
                    .ToList();

                foreach (var att in removeAttachments)
                {
                    if (System.IO.File.Exists(att.FilePath))
                        System.IO.File.Delete(att.FilePath);

                    // ONLY remove directly from DbSet
                    _context.Attachments.Remove(att);
                    changed = true;
                }

                // --------------------------
                // ADD NEW ATTACHMENTS
                // --------------------------
                if (model.NewAttachments != null && model.NewAttachments.Any())
                {
                    var uploadPath = Path.Combine("wwwroot", "uploads", "reports", report.Id.ToString());
                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in model.NewAttachments)
                    {
                        var uniqueName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadPath, uniqueName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        var newAttachment = new Attachment
                        {
                            Id = Guid.NewGuid(),
                            FileName = uniqueName,
                            FilePath = filePath,
                            ReportId = report.Id
                        };

                        // Add to navigation property
                        report.Attachments.Add(newAttachment);

                        // Explicitly track it so it's inserted
                        _context.Attachments.Add(newAttachment);
                    }

                    changed = true;
                }
            }

            // --------------------------
            // STATUS CHANGED?
            // --------------------------
            if (model.StatusId != lastStatusId)
            {
                changed = true;
            }

            // --------------------------
            // CREATE CHANGE LOG ENTRY
            // --------------------------
            if (changed)
            {
                var log = new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    UserId = userId!,
                    StatusId = lastStatusId,
                    Timestamp = DateTime.UtcNow,
                    ChangeDescription = isReporter ? "Report updated" : "Status updated"
                };

                _context.ChangeLogs.Add(log);
            }

            // --------------------------
            // SAVE CHANGES
            // --------------------------
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorToast"] = "This report was updated by someone else. Please reload.";
                return RedirectToAction("Edit", new { id = report.Id });
            }

            TempData["SuccessToast"] = "Report updated successfully.";
            return RedirectToAction("Details", new { id = report.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
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

            TempData["WarnToast"] = "Report deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}