namespace CursProject.Models
{
    public class UserDashboardViewModel
    {
        // Шаблоны, созданные пользователем
        public List<SurveyTemplate> MyTemplates { get; set; }
        // Заполненные формы (ответы) для шаблонов, созданных пользователем
        public List<SurveyResponse> MyResponses { get; set; }

        public IEnumerable<ApplicationUser> AllUsers { get; set; }

        public string SelectedUserId { get; set; }
    }
}
