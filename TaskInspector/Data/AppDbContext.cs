using Microsoft.EntityFrameworkCore;
using TaskInspector.Models;

namespace TaskInspector.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.Status);

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.Assignee);

            modelBuilder.Entity<TaskItem>()
                .Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}