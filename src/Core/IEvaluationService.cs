using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Models;

namespace ModelEvaluator.Core
{
    /// <summary>
    /// Main service for orchestrating model evaluations
    /// </summary>
    public interface IEvaluationService
    {
        /// <summary>
        /// Get all available model providers
        /// </summary>
        Task<IEnumerable<IModelProvider>> GetProvidersAsync();
        
        /// <summary>
        /// Get a specific provider by ID
        /// </summary>
        Task<IModelProvider?> GetProviderAsync(string providerId);
        
        /// <summary>
        /// Start a new evaluation session
        /// </summary>
        Task<EvaluationSession> StartSessionAsync(string? description = null);
        
        /// <summary>
        /// Evaluate a prompt using a specific model and provider
        /// </summary>
        Task<EvaluationResult> EvaluateAsync(
            string providerId, 
            string modelId, 
            string prompt, 
            EvaluationSession? session = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Evaluate a prompt with streaming response updates
        /// </summary>
        Task<EvaluationResult> EvaluateWithStreamingAsync(
            string providerId, 
            string modelId, 
            string prompt, 
            Action<string>? onStreamingUpdate = null,
            EvaluationSession? session = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Complete an evaluation session
        /// </summary>
        Task CompleteSessionAsync(EvaluationSession session);
        
        /// <summary>
        /// Generate a report for an evaluation session
        /// </summary>
        Task<string> GenerateReportAsync(EvaluationSession session, string format = "HTML", CancellationToken cancellationToken = default);
    }
}
