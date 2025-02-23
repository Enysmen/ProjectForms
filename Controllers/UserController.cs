using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CursProject.Data;
using CursProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace CursProject.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /User/Index
        public async Task<IActionResult> Index(string userId = null)
        {
            bool isAdmin = User.IsInRole("Administrator");
            string selectedUserId;

            if (isAdmin)
            {
                if (string.IsNullOrEmpty(userId))
                {
                    var allUsers = await _context.Users.ToListAsync();
                    if (allUsers.Any())
                    {
                        selectedUserId = allUsers.OrderBy(u => Guid.NewGuid()).First().Id;
                    }
                    else
                    {
                        return View(new UserDashboardViewModel());
                    }
                }
                else
                {
                    selectedUserId = userId;
                }
            }
            else
            {
                selectedUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            // Загружаем шаблоны, созданные выбранным пользователем
            var myTemplates = await _context.SurveyTemplates
                .Where(t => t.UserId == selectedUserId)
                .OrderBy(t => t.Title)
                .ToListAsync();

            // Загружаем все ответы для шаблонов, где создатель шаблона равен выбранному пользователю
            var allResponses = await _context.SurveyResponses
                .Include(r => r.SurveyTemplate)
                .Include(r => r.User)
                .Where(r => r.SurveyTemplate.UserId == selectedUserId)
                .ToListAsync();

            // Группируем ответы по SurveyTemplateId и выбираем из каждой группы последнюю по дате заполнения
            var myResponses = allResponses
                .GroupBy(r => r.SurveyTemplateId)
                .Select(g => g.OrderByDescending(r => r.SubmittedDate).First())
                .OrderBy(r => r.SurveyTemplate.Title)
                .ToList();

            var allUsersList = isAdmin
                ? await _context.Users.ToListAsync()
                : null;

            var model = new UserDashboardViewModel
            {
                MyTemplates = myTemplates,
                MyResponses = myResponses,
                AllUsers = allUsersList,
                SelectedUserId = selectedUserId
            };

            ViewBag.IsAdmin = isAdmin;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateThemePreference(string theme)
        {
            // Допустимые значения: "light" и "dark"
            if (string.IsNullOrEmpty(theme) || (theme.ToLower() != "light" && theme.ToLower() != "dark"))
            {
                return BadRequest("Invalid theme value.");
            }

            // Если пользователь не залогинен — пропускаем обновление (или возвращаем Ok)
            if (!User.Identity.IsAuthenticated)
            {
                return Ok(new { success = true });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            user.ThemePreference = theme.ToLower();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return StatusCode(500, "Could not update theme preference.");
            }
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLanguagePreference(string language)
        {
            // Допустимые значения: "en" и "ru"
            if (string.IsNullOrEmpty(language) || (language.ToLower() != "en" && language.ToLower() != "ru"))
            {
                return BadRequest("Invalid language value.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            user.LanguagePreference = language.ToLower();
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return StatusCode(500, "Could not update language preference.");
            }
            return Json(new { success = true });
        }
    }
}
