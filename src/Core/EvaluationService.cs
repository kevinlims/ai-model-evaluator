using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelEvaluator.Core;
using ModelEvaluator.Models;

namespace ModelEvaluator.Core
{
    /// <summary>
    /// Main service for orchestrating model evaluations
    /// </summary>
    public class EvaluationService : IEvaluationService
    {
        private readonly IEnumerable<IModelProvider> _providers;
        private readonly IMetricsCollector _metricsCollector;
        private readonly IEnumerable<IReportGenerator> _reportGenerators;
        private readonly ILogger<EvaluationService> _logger;

        public EvaluationService(
            IEnumerable<IModelProvider> providers,
            IMetricsCollector metricsCollector,
            IEnumerable<IReportGenerator> reportGenerators,
            ILogger<EvaluationService> logger)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _reportGenerators = reportGenerators ?? throw new ArgumentNullException(nameof(reportGenerators));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<IModelProvider>> GetProvidersAsync()
        {
            var availableProviders = new List<IModelProvider>();
            
            foreach (var provider in _providers)
            {
                try
                {
                    if (await provider.IsAvailableAsync())
                    {
                        availableProviders.Add(provider);
                        _logger.LogInformation("Provider {ProviderId} is available", provider.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Provider {ProviderId} is not available", provider.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking availability of provider {ProviderId}", provider.Id);
                }
            }

            return availableProviders;
        }

        public async Task<IModelProvider?> GetProviderAsync(string providerId)
        {
            var provider = _providers.FirstOrDefault(p => p.Id.Equals(providerId, StringComparison.OrdinalIgnoreCase));
            
            if (provider != null)
            {
                try
                {
                    if (await provider.IsAvailableAsync())
                    {
                        return provider;
                    }
                    
                    _logger.LogWarning("Provider {ProviderId} exists but is not available", providerId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking availability of provider {ProviderId}", providerId);
                }
            }
            
            return null;
        }

        public async Task<EvaluationSession> StartSessionAsync(string? description = null)
        {
            var session = new EvaluationSession
            {
                Description = description
            };

            // Collect device metadata
            try
            {
                session.DeviceInfo = DeviceMetadata.Collect();
                _logger.LogInformation("Collected device metadata for session {SessionId}", session.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect device metadata for session {SessionId}", session.Id);
            }

            _logger.LogInformation("Started evaluation session {SessionId}", session.Id);
            
            return await Task.FromResult(session);
        }

        public async Task<EvaluationResult> EvaluateAsync(
            string providerId, 
            string modelId, 
            string prompt, 
            EvaluationSession? session = null,
            CancellationToken cancellationToken = default)
        {
            var provider = await GetProviderAsync(providerId);
            if (provider == null)
            {
                return new EvaluationResult
                {
                    ProviderId = providerId,
                    ModelId = modelId,
                    Prompt = prompt,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    IsSuccess = false,
                    ErrorMessage = $"Provider '{providerId}' is not available"
                };
            }

            _logger.LogInformation("Starting evaluation with provider {ProviderId}, model {ModelId}", 
                providerId, modelId);

            EvaluationResult result;
            
            try
            {
                // Track AI model processes for enhanced metrics with provider context
                _metricsCollector.TrackProviderProcesses(providerId, modelId);
                
                // Start metrics collection
                await _metricsCollector.StartCollectionAsync(cancellationToken);

                // Perform the evaluation
                result = await provider.EvaluateAsync(modelId, prompt, cancellationToken);

                // Stop metrics collection and attach to result
                result.Metrics = await _metricsCollector.StopCollectionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during evaluation with provider {ProviderId}, model {ModelId}", 
                    providerId, modelId);

                result = new EvaluationResult
                {
                    ProviderId = providerId,
                    ModelId = modelId,
                    Prompt = prompt,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };

                // Attempt to stop metrics collection even on error
                try
                {
                    if (_metricsCollector.IsCollecting)
                    {
                        result.Metrics = await _metricsCollector.StopCollectionAsync(cancellationToken);
                    }
                }
                catch (Exception metricsEx)
                {
                    _logger.LogWarning(metricsEx, "Failed to stop metrics collection after evaluation error");
                }
            }

            // Add result to session if provided
            if (session != null)
            {
                session.Results.Add(result);
            }

            _logger.LogInformation("Completed evaluation with provider {ProviderId}, model {ModelId}. Success: {IsSuccess}, Duration: {Duration}ms",
                providerId, modelId, result.IsSuccess, result.Duration.TotalMilliseconds);

            return result;
        }

        public async Task CompleteSessionAsync(EvaluationSession session)
        {
            if (!session.IsCompleted)
            {
                session.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Completed evaluation session {SessionId} with {ResultCount} results. Duration: {Duration}",
                    session.Id, session.Results.Count, session.Duration?.TotalSeconds);
            }
            
            await Task.CompletedTask;
        }

        public async Task<string> GenerateReportAsync(EvaluationSession session, string format = "HTML", CancellationToken cancellationToken = default)
        {
            var reportGenerator = _reportGenerators.FirstOrDefault(g => 
                g.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

            if (reportGenerator == null)
            {
                throw new ArgumentException($"No report generator found for format '{format}'", nameof(format));
            }

            _logger.LogInformation("Generating {Format} report for session {SessionId}", format, session.Id);

            try
            {
                var report = await reportGenerator.GenerateReportAsync(session, cancellationToken);
                
                _logger.LogInformation("Successfully generated {Format} report for session {SessionId}", 
                    format, session.Id);
                
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Format} report for session {SessionId}", 
                    format, session.Id);
                throw;
            }
        }
    }
}
