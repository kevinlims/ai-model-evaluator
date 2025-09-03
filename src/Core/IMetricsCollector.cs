using System;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Models;

namespace ModelEvaluator.Core
{
    /// <summary>
    /// Interface for collecting system metrics during model evaluation
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Start collecting metrics
        /// </summary>
        Task StartCollectionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop collecting metrics and return the collected data
        /// </summary>
        Task<MetricsData> StopCollectionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Get current metrics snapshot
        /// </summary>
        Task<MetricsSnapshot> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if the metrics collector is currently collecting
        /// </summary>
        bool IsCollecting { get; }
        
        /// <summary>
        /// Track AI model processes for enhanced metrics collection
        /// </summary>
        void TrackModelProcesses(string modelId);
        
        /// <summary>
        /// Track AI model processes for a specific provider
        /// </summary>
        void TrackProviderProcesses(string providerId, string modelId);
    }
}
