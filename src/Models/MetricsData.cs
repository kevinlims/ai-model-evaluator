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
