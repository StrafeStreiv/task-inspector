using Microsoft.AspNetCore.Mvc;
using TaskInspector.DTOs;
using TaskInspector.Services;

namespace TaskInspector.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public AnalyticsController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<AnalyticsDto>> GetAnalytics()
        {
            var analytics = await _taskService.GetAnalyticsAsync();
            return Ok(analytics);
        }
    }
}