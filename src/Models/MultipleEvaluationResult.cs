using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelEvaluator.Models
{
    /// <summary>
    /// Represents the aggregated results of multiple evaluation runs
    /// </summary>
    public class MultipleEvaluationResult
    {
        public string ModelId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public List<EvaluationResult> IndividualResults { get; set; } = new();
        public int TotalRuns { get; set; }
        public int SuccessfulRuns { get; set; }
        public int FailedRuns { get; set; }
        public double SuccessRate { get; set; }
        
        // Aggregated timing metrics
        public TimeSpan AverageDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public TimeSpan TotalDuration { get; set; }
        
        // Aggregated performance metrics
        public AggregatedMetrics? AggregatedMetrics { get; set; }
        
        // Response analysis
        public List<string> UniqueResponses { get; set; } = new();
        public Dictionary<string, int> ResponseFrequency { get; set; } = new();
        public bool AllResponsesIdentical => UniqueResponses.Count <= 1;
        
        /// <summary>
        /// Aggregates multiple evaluation results into a single summary
        /// </summary>
        public static MultipleEvaluationResult FromResults(List<EvaluationResult> results)
        {
            if (results == null || !results.Any())
                throw new ArgumentException("Results cannot be null or empty", nameof(results));

            var firstResult = results.First();
            var successfulResults = results.Where(r => r.IsSuccess).ToList();
            
            var multipleResult = new MultipleEvaluationResult
            {
                ModelId = firstResult.ModelId,
                ProviderId = firstResult.ProviderId,
                Prompt = firstResult.Prompt,
                IndividualResults = results,
                TotalRuns = results.Count,
                SuccessfulRuns = successfulResults.Count,
                FailedRuns = results.Count - successfulResults.Count,
                SuccessRate = results.Count > 0 ? (double)successfulResults.Count / results.Count * 100 : 0
            };

            // Calculate timing metrics
            var durations = results.Select(r => r.Duration).ToList();
            multipleResult.AverageDuration = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
            multipleResult.MinDuration = durations.Min();
            multipleResult.MaxDuration = durations.Max();
            multipleResult.TotalDuration = TimeSpan.FromMilliseconds(durations.Sum(d => d.TotalMilliseconds));

            // Analyze responses
            var responses = successfulResults.Select(r => r.Response).Where(r => !string.IsNullOrEmpty(r)).ToList();
            multipleResult.UniqueResponses = responses.Distinct().ToList();
            multipleResult.ResponseFrequency = responses
                .GroupBy(r => r)
                .ToDictionary(g => g.Key, g => g.Count());

            // Aggregate metrics
            var metricsResults = successfulResults.Where(r => r.Metrics != null).Select(r => r.Metrics!).ToList();
            if (metricsResults.Any())
            {
                multipleResult.AggregatedMetrics = AggregatedMetrics.FromMetrics(metricsResults);
            }

            return multipleResult;
        }
    }

    /// <summary>
    /// Represents aggregated performance metrics across multiple runs
    /// </summary>
    public class AggregatedMetrics
    {
        // CPU metrics
        public double AverageCpuUsage { get; set; }
        public double MinCpuUsage { get; set; }
        public double MaxCpuUsage { get; set; }
        public double PeakCpuUsage { get; set; }
        
        // Memory metrics
        public double AverageMemoryUsageMB { get; set; }
        public double MinMemoryUsageMB { get; set; }
        public double MaxMemoryUsageMB { get; set; }
        public double PeakMemoryUsageMB { get; set; }
        
        // GPU metrics
        public double AverageGpuUsage { get; set; }
        public double MinGpuUsage { get; set; }
        public double MaxGpuUsage { get; set; }
        public double PeakGpuUsage { get; set; }
        
        // NPU metrics
        public double AverageNpuUsage { get; set; }
        public double MinNpuUsage { get; set; }
        public double MaxNpuUsage { get; set; }
        public double PeakNpuUsage { get; set; }

        /// <summary>
        /// Aggregates multiple metrics data into averaged results
        /// </summary>
        public static AggregatedMetrics FromMetrics(List<MetricsData> metrics)
        {
            if (metrics == null || !metrics.Any())
                throw new ArgumentException("Metrics cannot be null or empty", nameof(metrics));

            return new AggregatedMetrics
            {
                // CPU metrics
                AverageCpuUsage = metrics.Average(m => m.AverageCpuUsage),
                MinCpuUsage = metrics.Min(m => m.AverageCpuUsage),
                MaxCpuUsage = metrics.Max(m => m.AverageCpuUsage),
                PeakCpuUsage = metrics.Average(m => m.PeakCpuUsage),
                
                // Memory metrics
                AverageMemoryUsageMB = metrics.Average(m => m.AverageMemoryUsageMB),
                MinMemoryUsageMB = metrics.Min(m => m.AverageMemoryUsageMB),
                MaxMemoryUsageMB = metrics.Max(m => m.AverageMemoryUsageMB),
                PeakMemoryUsageMB = metrics.Average(m => m.PeakMemoryUsageMB),
                
                // GPU metrics
                AverageGpuUsage = metrics.Average(m => m.AverageGpuUsage),
                MinGpuUsage = metrics.Min(m => m.AverageGpuUsage),
                MaxGpuUsage = metrics.Max(m => m.AverageGpuUsage),
                PeakGpuUsage = metrics.Average(m => m.PeakGpuUsage),
                
                // NPU metrics
                AverageNpuUsage = metrics.Average(m => m.AverageNpuUsage),
                MinNpuUsage = metrics.Min(m => m.AverageNpuUsage),
                MaxNpuUsage = metrics.Max(m => m.AverageNpuUsage),
                PeakNpuUsage = metrics.Average(m => m.PeakNpuUsage)
            };
        }
    }
}
