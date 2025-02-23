using System.Reflection.Emit;

using CursProject.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CursProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public DbSet<SurveyTemplate> SurveyTemplates { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<TemplateComment> TemplateComments { get; set; }
        public DbSet<TemplateLike> TemplateLikes { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().HasIndex(u => u.NormalizedEmail).IsUnique();

        }
    }
}
