using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TaskInspector.Data;
using TaskInspector.DTOs;
using TaskInspector.Models;
using TaskInspector.Services;


namespace TaskInspector.Tests
{
    public class TaskServiceTests
    {
        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private TaskService CreateService(AppDbContext context)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AllowedAssignees:0"] = "Иван",
                    ["AllowedAssignees:1"] = "Мария"
                })
                .Build();
            return new TaskService(context, config, NullLogger<TaskService>.Instance);
        }

        [Fact]
        public async Task CreateTask_ValidDto_CreatesTask()
        {
            // Arrange
            using var context = CreateContext();
            var service = CreateService(context);
            var dto = new CreateTaskDto
            {
                Title = "Test Task",
                Description = "Desc",
                Priority = Priority.Medium,
                Deadline = DateTime.UtcNow.AddDays(1),
                Assignee = "Иван"
            };

            // Act
            var result = await service.CreateTaskAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Task", result.Title);
            Assert.Equal(Status.Open, result.Status);
            Assert.Equal(1, await context.Tasks.CountAsync());
        }

        [Fact]
        public async Task CreateTask_InvalidAssignee_Throws()
        {
            using var context = CreateContext();
            var service = CreateService(context);
            var dto = new CreateTaskDto
            {
                Title = "Test",
                Priority = Priority.Low,
                Deadline = DateTime.UtcNow.AddDays(1),
                Assignee = "Неизвестный"
            };

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTaskAsync(dto));
        }

        [Fact]
        public async Task UpdateStatus_ExceedsLimit_Throws()
        {
            using var context = CreateContext();
            var service = CreateService(context);

            // Создаём три задачи в работе для Ивана
            for (int i = 0; i < 3; i++)
            {
                context.Tasks.Add(new TaskItem
                {
                    Title = $"Task {i}",
                    Assignee = "Иван",
                    Status = Status.InProgress,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // Создаём открытую задачу для Ивана
            var openTask = new TaskItem
            {
                Title = "Open Task",
                Assignee = "Иван",
                Status = Status.Open,
                CreatedAt = DateTime.UtcNow
            };
            context.Tasks.Add(openTask);
            await context.SaveChangesAsync();

            var dto = new UpdateStatusDto { Status = Status.InProgress };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateStatusAsync(openTask.Id, dto));
        }
    }
}