using System.ComponentModel.DataAnnotations;

namespace TaskInspector.Models
{

    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Priority Priority { get; set; }

        public Status Status { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public string Assignee { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? Comment { get; set; }
    }
}