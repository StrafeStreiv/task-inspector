using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskInspector.Data;
using TaskInspector.DTOs;
using TaskInspector.Models;
using System.Text.Json;
using TaskInspector.DTO;

namespace TaskInspector.Services
{

    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TaskService> _logger;
        private readonly string[] _allowedAssignees;

        public TaskService(AppDbContext context, IConfiguration configuration, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
            _allowedAssignees = configuration.GetSection("AllowedAssignees").Get<string[]>()
                                ?? throw new InvalidOperationException("AllowedAssignees not configured");
        }

        public async Task<IEnumerable<TaskDto>> GetTasksAsync(Status? status, string? assignee)
        {
            var query = _context.Tasks.AsNoTracking();

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(assignee))
                query = query.Where(t => t.Assignee == assignee);

            var tasks = await query.ToListAsync();
            return tasks.Select(MapToDto);
        }

        public async Task<TaskDto?> GetTaskByIdAsync(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            return task == null ? null : MapToDto(task);
        }

        public async Task<TaskDto> CreateTaskAsync(CreateTaskDto dto)
        {
            // Валидация исполнителя
            if (!_allowedAssignees.Contains(dto.Assignee))
                throw new ArgumentException($"Исполнитель '{dto.Assignee}' не найден в списке допустимых.");

            // Проверка срока
            if (dto.Deadline.HasValue && dto.Deadline.Value < DateTime.UtcNow)
                throw new ArgumentException("Срок выполнения не может быть в прошлом.");

            // Для Critical задач: если срок не указан, ставим +24ч
            var deadline = dto.Deadline;
            if (dto.Priority == Priority.Critical && !deadline.HasValue)
            {
                deadline = DateTime.UtcNow.AddHours(24);
            }

            var task = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                Deadline = deadline,
                Assignee = dto.Assignee,
                Status = Status.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created task {TaskId} for assignee {Assignee}", task.Id, task.Assignee);
            return MapToDto(task);
        }

        public async Task<TaskDto> UpdateStatusAsync(int id, UpdateStatusDto dto)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                throw new KeyNotFoundException($"Задача с id {id} не найдена.");

            // Проверка допустимости перехода
            if (task.Status == Status.Completed)
                throw new InvalidOperationException("Нельзя изменить статус выполненной задачи.");

            if (task.Status == dto.Status)
                return MapToDto(task); // ничего не меняем

            // Разрешены только последовательные переходы
            bool validTransition = (task.Status == Status.Open && dto.Status == Status.InProgress) ||
                                   (task.Status == Status.InProgress && dto.Status == Status.Completed);
            if (!validTransition)
                throw new InvalidOperationException($"Недопустимый переход из {task.Status} в {dto.Status}.");

            // Проверка лимита InProgress
            if (dto.Status == Status.InProgress)
            {
                var inProgressCount = await _context.Tasks
                    .CountAsync(t => t.Assignee == task.Assignee && t.Status == Status.InProgress);

                if (inProgressCount >= 3)
                    throw new InvalidOperationException($"У исполнителя {task.Assignee} уже 3 задачи в работе. Сначала завершите одну из них.");
            }

            // Обновляем статус и, если завершена, устанавливаем CompletedAt
            task.Status = dto.Status;
            if (dto.Status == Status.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {TaskId} status updated to {Status}", task.Id, task.Status);
            return MapToDto(task);
        }

        public async Task<TaskDto> AddCommentAsync(int id, AddCommentDto dto)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                throw new KeyNotFoundException($"Задача с id {id} не найдена.");

            task.Comment = dto.Comment;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Comment added to task {TaskId}", task.Id);
            return MapToDto(task);
        }

        public async Task<AnalyticsDto> GetAnalyticsAsync()
        {
            var now = DateTime.UtcNow;

            var totalTasks = await _context.Tasks.CountAsync();
            var openCount = await _context.Tasks.CountAsync(t => t.Status == Status.Open);
            var inProgressCount = await _context.Tasks.CountAsync(t => t.Status == Status.InProgress);
            var completedCount = await _context.Tasks.CountAsync(t => t.Status == Status.Completed);
            var overdueCount = await _context.Tasks
                .CountAsync(t => t.Status != Status.Completed && t.Deadline < now);

            // Среднее время выполнения для завершённых задач (в часах)
            double? avgHours = null;
            if (completedCount > 0)
            {
                avgHours = await _context.Tasks
                    .Where(t => t.Status == Status.Completed && t.CompletedAt.HasValue)
                    .AverageAsync(t => EF.Functions.DateDiffHour(t.CreatedAt, t.CompletedAt.Value));
            }

            // Исполнитель с наибольшим количеством просроченных задач
            var topOverdueAssignee = await _context.Tasks
                .Where(t => t.Status != Status.Completed && t.Deadline < now)
                .GroupBy(t => t.Assignee)
                .Select(g => new { Assignee = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            return new AnalyticsDto
            {
                TotalTasks = totalTasks,
                OpenCount = openCount,
                InProgressCount = inProgressCount,
                CompletedCount = completedCount,
                OverdueCount = overdueCount,
                AverageCompletionTimeHours = avgHours,
                TopOverdueAssignee = topOverdueAssignee?.Assignee
            };
        }

        private TaskDto MapToDto(TaskItem task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                Status = task.Status,
                Deadline = task.Deadline,
                Assignee = task.Assignee,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                Comment = task.Comment,
                IsOverdue = task.Status != Status.Completed && task.Deadline < DateTime.UtcNow
            };
        }S
    }
}