using AvaloniaManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AvaloniaManager.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Article> Articles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                connectionString: "Server=INFO\\SQLEXPRESS;Database=employeeArticlesManager;User Id=app_user;Password=app_user_password;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка ограничений для Employee
            modelBuilder.Entity<Employee>()
                .Property(e => e.ContractName)
                .HasConversion<string>()
                .HasMaxLength(80);

            // Настройка ограничений для Article
            modelBuilder.Entity<Article>()
                .Property(a => a.SMI)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Article>()
                .Property(a => a.ContentType)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Связь между Article и Employee
            modelBuilder.Entity<Article>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Articles)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}