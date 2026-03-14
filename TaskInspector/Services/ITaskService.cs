using TaskInspector.DTO;
using TaskInspector.DTOs;
using TaskInspector.Models;

namespace TaskInspector.Services
{

    public interface ITaskService
    {
        Task<IEnumerable<TaskDto>> GetTasksAsync(Status? status, string? assignee);
        Task<TaskDto?> GetTaskByIdAsync(int id);
        Task<TaskDto> CreateTaskAsync(CreateTaskDto dto);
        Task<TaskDto> UpdateStatusAsync(int id, UpdateStatusDto dto);
        Task<TaskDto> AddCommentAsync(int id, AddCommentDto dto);
        Task<AnalyticsDto> GetAnalyticsAsync();
    }
}