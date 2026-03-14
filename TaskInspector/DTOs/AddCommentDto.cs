using System.ComponentModel.DataAnnotations;

namespace TaskInspector.DTOs
{

    public class AddCommentDto
    {
        [Required]
        public string Comment { get; set; } = string.Empty;
    }
}