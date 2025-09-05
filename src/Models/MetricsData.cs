using System;
using System.Collections.Generic;

namespace ModelEvaluator.Models
{
    /// <summary>
    /// Represents collected metrics data over time
    /// </summary>
    public class MetricsData
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public List<MetricsSnapshot> Snapshots { get; set; } = new();
        
        // Aggregated metrics
        public double AverageCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        public double AverageMemoryUsageMB { get; set; }
        public double PeakMemoryUsageMB { get; set; }
        public double AverageGpuUsage { get; set; }
        public double PeakGpuUsage { get; set; }
        public double AverageNpuUsage { get; set; }
        public double PeakNpuUsage { get; set; }
        
        // Token performance metrics
        public TimeSpan? TimeToFirstToken { get; set; }
        public double? TokensPerSecond { get; set; }
        public int? TotalTokens { get; set; }
        public int? PromptTokens { get; set; }
        public int? CompletionTokens { get; set; }
        
        /// <summary>
        /// Calculates token performance metrics based on timing and token data
        /// </summary>
        /// <param name="firstTokenTime">Time when first token was received</param>
        /// <param name="totalDuration">Total duration of the evaluation</param>
        /// <param name="totalTokens">Total number of tokens generated</param>
        /// <param name="promptTokens">Number of tokens in the prompt</param>
        /// <param name="completionTokens">Number of tokens in the completion</param>
        public void CalculateTokenMetrics(DateTime? firstTokenTime, TimeSpan totalDuration, int? totalTokens = null, int? promptTokens = null, int? completionTokens = null)
        {
            if (firstTokenTime.HasValue && StartTime != default)
            {
                TimeToFirstToken = firstTokenTime.Value - StartTime;
            }
            
            if (completionTokens.HasValue && completionTokens > 0 && totalDuration.TotalSeconds > 0)
            {
                TokensPerSecond = completionTokens.Value / totalDuration.TotalSeconds;
            }
            
            TotalTokens = totalTokens;
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
        }
    }
    
    /// <summary>
    /// Represents a single point-in-time metrics snapshot
    /// </summary>
    public class MetricsSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public double GpuUsagePercent { get; set; }
        public double GpuMemoryUsageMB { get; set; }
        public double NpuUsagePercent { get; set; }
        public double NetworkBytesPerSecond { get; set; }
        public double DiskBytesPerSecond { get; set; }
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}
