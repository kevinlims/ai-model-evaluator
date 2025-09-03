using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Models;

namespace ModelEvaluator.Core
{
    /// <summary>
    /// Represents an AI model provider that can evaluate prompts
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// Unique identifier for the provider
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Display name for the provider
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Description of the provider
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// List of available models from this provider
        /// </summary>
        Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Evaluate a prompt using the specified model
        /// </summary>
        Task<EvaluationResult> EvaluateAsync(string modelId, string prompt, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if the provider is available and properly configured
        /// </summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}
