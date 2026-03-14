using Microsoft.AspNetCore.Mvc;
using TaskInspector.DTOs;
using TaskInspector.Models;
using TaskInspector.Services;

namespace TaskInspector.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks(
            [FromQuery] Status? status,
            [FromQuery] string? assignee)
        {
            var tasks = await _taskService.GetTasksAsync(status, assignee);
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto dto)
        {
            try
            {
                var task = await _taskService.CreateTaskAsync(dto);
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<TaskDto>> UpdateStatus(int id, UpdateStatusDto dto)
        {
            try
            {
                var task = await _taskService.UpdateStatusAsync(id, dto);
                return Ok(task);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<TaskDto>> AddComment(int id, AddCommentDto dto)
        {
            try
            {
                var task = await _taskService.AddCommentAsync(id, dto);
                return Ok(task);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}