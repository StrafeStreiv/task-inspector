namespace TaskInspector.DTOs
{

    public class AnalyticsDto
    {
        public int TotalTasks { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public double? AverageCompletionTimeHours { get; set; } // среднее время в часах
        public string? TopOverdueAssignee { get; set; }
    }
}