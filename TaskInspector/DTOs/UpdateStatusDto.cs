using System.ComponentModel.DataAnnotations;
using TaskInspector.Models;

namespace TaskInspector.DTOs
{
    public class UpdateStatusDto
    {
        [Required]
        public Status Status { get; set; }
    }
}