using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CursProject.Data;
using CursProject.Models;
using System.Security.Claims;

namespace CursProject.Controllers
{
    // Доступно для всех пользователей для просмотра формы
    [AllowAnonymous]
    public class SurveyController : Controller
    {
        private readonly ApplicationDbContext _context;
        public SurveyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Survey/Fill/5
        public async Task<IActionResult> Fill(int id)
        {
            // Загружаем шаблон (без комментариев и лайков)
            var template = await _context.SurveyTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            // Передаем JSON-схему и id шаблона
            ViewBag.JsonSchema = template.JsonSchema;
            ViewBag.TemplateId = template.Id;

            // Если пользователь не залогинен, переводим форму в режим "display" (только для просмотра)
            ViewBag.IsReadOnly = !User.Identity.IsAuthenticated;

            return View();
        }

        // Остальные действия (Submit, ThankYou) остаются без изменений
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Submit(int templateId, string responseData)
        {
            var response = new SurveyResponse
            {
                SurveyTemplateId = templateId,
                ResponseData = responseData,
                SubmittedDate = System.DateTime.UtcNow,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            _context.SurveyResponses.Add(response);
            await _context.SaveChangesAsync();
            return RedirectToAction("ThankYou");
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        // Для переключения лайка можно реализовать отдельное действие (например, ToggleLike) – если требуется
        // Например:
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int templateId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var template = await _context.SurveyTemplates
                .Include(t => t.Likes)
                .FirstOrDefaultAsync(t => t.Id == templateId);
            if (template == null)
                return NotFound();

            var existingLike = template.Likes?.FirstOrDefault(l => l.UserId == currentUserId);
            if (existingLike == null)
            {
                // Добавляем лайк
                template.Likes.Add(new TemplateLike
                {
                    SurveyTemplateId = templateId,
                    UserId = currentUserId,
                    CreatedDate = System.DateTime.UtcNow
                });
            }
            else
            {
                // Удаляем лайк
                _context.TemplateLikes.Remove(existingLike);
            }
            await _context.SaveChangesAsync();
            // После обновления перенаправляем обратно на Fill
            return RedirectToAction("Details", "SurveyTemplate", new { id = templateId });
        }

        // Для добавления комментария можно реализовать отдельное действие:
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int templateId, string commentText)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var comment = new TemplateComment
            {
                SurveyTemplateId = templateId,
                CommentText = commentText,
                UserId = currentUserId,
                CreatedDate = System.DateTime.UtcNow
            };
            _context.TemplateComments.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "SurveyTemplate", new { id = templateId });
        }
    }
}
