using TaskInspector.Models;

namespace TaskInspector.DTOs
{

    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; }
        public Status Status { get; set; }
        public DateTime? Deadline { get; set; }
        public string Assignee { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Comment { get; set; }
        public bool IsOverdue { get; set; }
    }
}