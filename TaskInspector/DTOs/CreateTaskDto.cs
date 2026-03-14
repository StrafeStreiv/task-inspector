using System.ComponentModel.DataAnnotations;
using TaskInspector.Models;

namespace TaskInspector.DTO
{

    public class CreateTaskDto
    {
        [Required]
        [StringLength(200, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public Priority Priority { get; set; }

        public DateTime? Deadline { get; set; }

        [Required]
        public string Assignee { get; set; } = string.Empty;
    }
}