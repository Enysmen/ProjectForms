using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CursProject.Data;
using CursProject.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;

namespace CursProject.Controllers
{
    public class SurveyTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SurveyTemplateController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /SurveyTemplate/Index?filter=all|mine|others
        
        public async Task<IActionResult> Index(string filter = "all")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Получаем все шаблоны с включением создателя
            IQueryable<SurveyTemplate> allQuery = _context.SurveyTemplates.Include(t => t.User);
            if (filter == "mine")
                allQuery = allQuery.Where(t => t.UserId == userId);
            else if (filter == "others")
                allQuery = allQuery.Where(t => t.UserId != userId);
            var allTemplates = await allQuery.ToListAsync();

            // Популярные шаблоны – топ-5 по количеству лайков
            var popularTemplates = await _context.SurveyTemplates
                .Include(t => t.Likes)
                .Include(t => t.User)
                .OrderByDescending(t => t.Likes.Count())
                .Take(5)
                .ToListAsync();

            var model = new SurveyTemplateIndexViewModel
            {
                PopularTemplates = popularTemplates,
                AllTemplates = allTemplates,
                Filter = filter
            };



            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var template = await _context.SurveyTemplates
                .Include(t => t.User)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Likes)
                .Include(t => t.Responses).ThenInclude(r => r.User)
                .Include(t => t.AllowedUsers)
                .FirstOrDefaultAsync(t => t.Id == id);


            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.TemplateId = template.Id;
            ViewBag.LikesCount = template.Likes?.Count() ?? 0;
            ViewBag.UserLiked = (currentUserId != null) && (template.Likes != null && template.Likes.Any(l => l.UserId == currentUserId));
            ViewBag.Comments = template.Comments?.OrderBy(c => c.CreatedDate).ToList();
            ViewBag.JsonSchema = template.JsonSchema;
            ViewBag.AllUsers = await _context.Users.ToListAsync();

            if (template == null)
            {
                return NotFound();
            }

            // Если JSON-схема отсутствует, можно вернуть ошибку или указать, что шаблон некорректный.
            if (string.IsNullOrEmpty(template.JsonSchema))
            {
                return Content("Ошибка: шаблон не содержит JSON-схему.");
            }

           

            return View(template);
        }

        // GET: /SurveyTemplate/Create
        // Доступно для всех; если пользователь не залогинен, страница работает в режиме "read-only"
        [Authorize]
        public async Task<IActionResult> Create(int? cloneTemplateId)
        {
            string initialSchema;
            if (cloneTemplateId.HasValue)
            {
                var templateToClone = await _context.SurveyTemplates.FindAsync(cloneTemplateId.Value);
                if (templateToClone != null)
                {
                    // Используем JSON-схему выбранного шаблона для клонирования
                    initialSchema = templateToClone.JsonSchema;
                }
                else
                {
                    // Если шаблон не найден, задаём базовую схему по умолчанию
                    initialSchema = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        title = "Новый опрос",
                        description = "Введите описание опроса",
                        elements = new object[] { }
                    });
                }
            }
            else
            {
                initialSchema = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    title = "Новый опрос",
                    description = "Введите описание опроса",
                    elements = new object[] { }
                });
            }


            var model = new SurveyTemplate
            {
                CreatedDate = DateTime.UtcNow,
                IsPublic = true,
                JsonSchema = initialSchema
            };

            // Передаём список всех пользователей для выбора (можно дополнительно исключить текущего пользователя)
            var allUsers = await _context.Users.ToListAsync();
            ViewBag.AllUsers = allUsers;

            return View(model);
        }

        // POST: /SurveyTemplate/Create
        // Это действие доступно только залогиненным пользователям (требуется [Authorize])
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SurveyTemplate surveyTemplate, string[] AllowedUserIds)
        {
            // Убираем ошибки валидации для Title и Description, если они заполняются из JSON-схемы
            ModelState.Remove("Title");
            ModelState.Remove("Description");

            if (ModelState.IsValid)
            {
                // Если модель не валидна, возвращаем форму для исправления
                return View(surveyTemplate);
            }
            ViewBag.AllUsers = await _context.Users.ToListAsync();
            
            surveyTemplate.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            surveyTemplate.CreatedDate = DateTime.UtcNow;

            // Если выбран режим Private (IsPublic == false) и переданы пользователи для доступа
            if (!surveyTemplate.IsPublic && AllowedUserIds != null && AllowedUserIds.Any())
            {
                surveyTemplate.AllowedUsers = new List<ApplicationUser>();
                foreach (var userId in AllowedUserIds)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        surveyTemplate.AllowedUsers.Add(user);
                    }
                }
            }

            // Здесь можно добавить дополнительную обработку JSON-схемы, например, заполнить Title и Description из JSON, если нужно
            try
            {
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(surveyTemplate.JsonSchema);
                surveyTemplate.Title = jsonObj.title != null ? (string)jsonObj.title : surveyTemplate.Title;
                surveyTemplate.Description = jsonObj.description != null ? (string)jsonObj.description : surveyTemplate.Description;
            }
            catch
            {
                // Если произошла ошибка, можно оставить введённые значения или задать значения по умолчанию
            }

            _context.Add(surveyTemplate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /SurveyTemplate/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var surveyTemplate = await _context.SurveyTemplates
                .Include(t => t.User)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Likes)
                .Include(t => t.Responses).ThenInclude(r => r.User)
                .Include(t => t.AllowedUsers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (surveyTemplate == null)
                return NotFound();



            var currentUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.TemplateId = surveyTemplate.Id;
            ViewBag.JsonSchema = surveyTemplate.JsonSchema;
            ViewBag.AllUsers = await _context.Users.ToListAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Если текущий пользователь не администратор и не является владельцем, запрещаем доступ
            if (!User.IsInRole("Administrator") && surveyTemplate.UserId != userId && (surveyTemplate.AllowedUsers == null || !surveyTemplate.AllowedUsers.Any(u => u.Id == userId)))
            {
                return Forbid();
            }

            return View(surveyTemplate);
        }

        // POST: /SurveyTemplate/Edit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SurveyTemplate surveyTemplate)
        {
            if (id != surveyTemplate.Id)
                return NotFound();

            // Убираем ошибки валидации для Title и Description,
            // так как они будут заполнены из JSON-схемы
            ModelState.Remove("Title");
            ModelState.Remove("Description");

            if (!ModelState.IsValid)
            {
                try
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    // Загружаем исходный шаблон из базы данных
                    var existingTemplate = await _context.SurveyTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTemplate == null)
                        return NotFound();

                    // Если текущий пользователь не администратор и не владелец, запрещаем редактирование
                    if (!User.IsInRole("Administrator") && existingTemplate.UserId != currentUserId)
                        return Forbid();

                    // Попытка распарсить JSON-схему и обновить Title и Description
                    try
                    {
                        dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(surveyTemplate.JsonSchema);
                        surveyTemplate.Title = jsonObj.title != null ? (string)jsonObj.title : "";
                        surveyTemplate.Description = jsonObj.description != null ? (string)jsonObj.description : "";
                    }
                    catch
                    {
                        surveyTemplate.Title = "Новый опрос";
                        surveyTemplate.Description = "Введите описание опроса";
                    }

                    // Сохраняем неизменными поля владельца и дату создания
                    surveyTemplate.UserId = existingTemplate.UserId;
                    surveyTemplate.CreatedDate = existingTemplate.CreatedDate;

                    // Обновляем сущность в базе
                    _context.Update(surveyTemplate);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "SurveyTemplate");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SurveyTemplateExists(surveyTemplate.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(surveyTemplate);
        }

        [Authorize]
        private bool SurveyTemplateExists(int id)
        {
            return _context.SurveyTemplates.Any(e => e.Id == id);
        }

        // POST: /SurveyTemplate/SelectPopularTemplate
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult SelectPopularTemplate(int? selectedTemplateId)
        {
            if (selectedTemplateId == null)
                return RedirectToAction("Create");
            return RedirectToAction("Create", new { cloneTemplateId = selectedTemplateId });
        }

        // POST: /SurveyTemplate/ProcessTemplate
        [HttpPost]
        [Authorize]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessTemplate(int? selectedTemplateId, string actionType, string filter)
        {

            // Сохраняем фильтр для последующего использования
            TempData["Filter"] = filter;



            // Если шаблон не выбран и не задано действие – это, скорее всего, изменение фильтра
            if (selectedTemplateId == null && string.IsNullOrEmpty(actionType))
            {
                return RedirectToAction("Index", new { filter = filter });
            }

            // Если шаблон не выбран
            if (selectedTemplateId == null)
            {
                if (actionType == "details")
                {
                    // Если пытаемся посмотреть шаблон, а он не выбран – выводим сообщение об ошибке и возвращаемся на Index
                    TempData["ErrorMessage"] = "Шаблон не выбран.";
                    return RedirectToAction("Index", new { filter = filter });
                }
                else
                {
                    // Для других действий перенаправляем на страницу создания нового шаблона
                    return RedirectToAction("Create");
                }
            }


            // Загружаем шаблон по выбранному ID
            var template = await _context.SurveyTemplates
                        .Include(t => t.AllowedUsers)
                        .FirstOrDefaultAsync(t => t.Id == selectedTemplateId);

            if (template == null)
            {
                return NotFound();
            }

            if (actionType == "create")
            {
                if (selectedTemplateId != null)
                {
                    return RedirectToAction("Create", new { cloneTemplateId = selectedTemplateId });
                }
                else
                {
                    return RedirectToAction("Create");
                }
            }

            // Если действие "details" – перенаправляем на страницу просмотра шаблона (Details.cshtml)
            if (actionType == "details")
            {
                return RedirectToAction("Details", new { id = selectedTemplateId });
            }

            // Для остальных действий проверяем, что пользователь аутентифицирован
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Если действие "fill" – перенаправляем на страницу заполнения формы
            if (actionType == "fill")
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!template.IsPublic)
                {

                    if (!User.IsInRole("Administrator") &&
                        !((template.AllowedUsers != null && template.AllowedUsers.Any(u => u.Id == currentUserId)) ||
                          template.UserId == currentUserId))
                    {
                        TempData["ErrorMessage"] = "У вас нет доступа для заполнения этой формы.";
                        return RedirectToAction("Index", new { filter = filter });
                    }
                }
                return RedirectToAction("Fill", "Survey", new { id = selectedTemplateId });
            }
            // Если действие "edit" – проверяем права и переходим на редактирование
            else if (actionType == "edit")
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!User.IsInRole("Administrator") && !(template.UserId == currentUserId || (template.AllowedUsers != null && template.AllowedUsers.Any(u => u.Id == currentUserId))))
                {
                    TempData["ErrorMessage"] = "У вас нет прав на редактирование этой формы.";
                    return RedirectToAction("Index", new { filter = filter });
                }
                return RedirectToAction("Edit", new { id = selectedTemplateId });
            }


            // Если ни один из вариантов не подошёл, возвращаемся на Index с фильтром
            return RedirectToAction("Index", new { filter = filter });
        }
        [Authorize]
        public async Task<IActionResult> Responses(int id)
        {
            var template = await _context.SurveyTemplates
                .Include(t => t.Responses)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (template.UserId != userId && !User.IsInRole("Administrator"))
            {
                TempData["ErrorMessage"] = "У вас нет прав на просмотр ответов этой формы.";
                return RedirectToAction("Index");
            }
            return View(template);
        }

        // POST: /SurveyTemplate/Delete/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Загружаем шаблон вместе со связанными данными (лайки, комментарии, ответы)
            var template = await _context.SurveyTemplates
                .Include(t => t.Likes)
                .Include(t => t.Comments)
                .Include(t => t.Responses)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            // Проверяем, является ли текущий пользователь владельцем шаблона, 
            // если нет, то разрешаем удаление только администратору
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") && template.UserId != userId)
                return Forbid();

            // Удаляем связанные объекты
            if (template.Likes != null && template.Likes.Any())
            {
                _context.TemplateLikes.RemoveRange(template.Likes);
            }
            if (template.Comments != null && template.Comments.Any())
            {
                _context.TemplateComments.RemoveRange(template.Comments);
            }
            if (template.Responses != null && template.Responses.Any())
            {
                _context.SurveyResponses.RemoveRange(template.Responses);
            }

            // Удаляем сам шаблон
            _context.SurveyTemplates.Remove(template);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetComments(int templateId)
        {
            var comments = await _context.TemplateComments
                .Include(c => c.User) // <-- добавляем Include, чтобы подгрузить пользователя
                .Where(c => c.SurveyTemplateId == templateId)
                .OrderBy(c => c.CreatedDate)
                .ToListAsync();
            return PartialView("_CommentsPartial", comments);
        }

        [Authorize]
        public async Task<IActionResult> AggregatedResponses(int id, string userId = null)
        {
            // Загружаем шаблон с ответами
            var template = await _context.SurveyTemplates
                .Include(t => t.Responses)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (template == null)
                return NotFound();

            // Если передан userId, фильтруем ответы по пользователю
            var responses = template.Responses.AsQueryable();
            if (!string.IsNullOrEmpty(userId))
            {
                responses = responses.Where(r => r.UserId == userId);
            }
            var responseList = await responses.ToListAsync();

            // Здесь можно добавить логику агрегации ответов, например, вычисление среднего значения для числовых полей.
            // Для примера передадим все ответы.
            var model = new AggregatedResponsesViewModel
            {
                Template = template,
                Responses = responseList
            };

            return View(model);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToTemplate(int templateId, string userId)
        {
            // Загружаем шаблон вместе с коллекцией AllowedUsers
            var template = await _context.SurveyTemplates
                .Include(t => t.AllowedUsers)
                .FirstOrDefaultAsync(t => t.Id == templateId);
            if (template == null)
            {
                return NotFound();
            }

            // Если коллекция ещё не инициализирована, создаём её
            if (template.AllowedUsers == null)
            {
                template.AllowedUsers = new List<ApplicationUser>();
            }

            // Проверяем, не добавлен ли уже выбранный пользователь
            if (!template.AllowedUsers.Any(u => u.Id == userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }
                template.AllowedUsers.Add(user);
                await _context.SaveChangesAsync();
            }

            // Можно вернуть PartialView с обновлённым списком пользователей или просто выполнить redirect
            return RedirectToAction("Details", new { id = templateId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTemplate(int id, string jsonSchema, bool isPublic)
        {
            var template = await _context.SurveyTemplates.FindAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            // Проверка прав: разрешаем редактирование владельцу, администратору или тем, кому предоставлен доступ
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Administrator") &&
                template.UserId != currentUserId &&
                (template.AllowedUsers == null || !template.AllowedUsers.Any(u => u.Id == currentUserId)))
            {
                return Forbid();
            }

            // Обновляем JSON-схему и настройку доступа
            template.JsonSchema = jsonSchema;
            template.IsPublic = isPublic;

            _context.Update(template);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        [Authorize]
        public async Task<IActionResult> ViewResponse(int id)
        {
            // Загружаем ответ вместе с шаблоном (чтобы получить JSON-схему)
            var response = await _context.SurveyResponses
                .Include(r => r.SurveyTemplate)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (response == null)
                return NotFound();

            return View("Responses", response);
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrEmpty(culture) || (culture.ToLower() != "en" && culture.ToLower() != "ru"))
            {
                culture = "en"; // Значение по умолчанию
            }
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return LocalRedirect(returnUrl);
        }
    }
}
