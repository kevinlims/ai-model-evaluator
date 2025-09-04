using System;
using System.Collections.Generic;

namespace ModelEvaluator.Models
{
    /// <summary>
    /// Represents a complete evaluation session with multiple evaluations
    /// </summary>
    public class EvaluationSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public List<EvaluationResult> Results { get; set; } = new();
        public Dictionary<string, object> Configuration { get; set; } = new();
        public string? Description { get; set; }
        public DeviceMetadata? DeviceInfo { get; set; }
        
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public bool IsCompleted => EndTime.HasValue;
    }
}
