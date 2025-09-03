using System;
using System.Collections.Generic;

namespace ModelEvaluator.Models
{
    /// <summary>
    /// Represents the result of a model evaluation
    /// </summary>
    public class EvaluationResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public MetricsData? Metrics { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
