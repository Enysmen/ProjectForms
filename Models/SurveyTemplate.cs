using System.ComponentModel.DataAnnotations;

namespace CursProject.Models
{
    public class SurveyTemplate
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }         // Значение будет обновляться из JSON

        public string Description { get; set; }     // То же самое – из JSON

        // JSON-схема, созданная в SurveyJS Creator
        [Required]
        public string JsonSchema { get; set; }

        public bool IsPublic { get; set; }



        public DateTime CreatedDate { get; set; }

        // Id пользователя (создателя шаблона)
        public string UserId { get; set; }

        // Навигационное свойство к пользователю
        public ApplicationUser User { get; set; }

        // Если потребуется – список ответов
        public ICollection<SurveyResponse> Responses { get; set; }

        // Добавим коллекции для комментариев и лайков
        public ICollection<TemplateComment> Comments { get; set; }
        public ICollection<TemplateLike> Likes { get; set; }

        public ICollection<ApplicationUser> AllowedUsers { get; set; }
    }
}
