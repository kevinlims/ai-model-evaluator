using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelEvaluator.Core;
using ModelEvaluator.Models;

namespace ModelEvaluator.UI
{
    /// <summary>
    /// Command-line interface for the model evaluator
    /// </summary>
    public class CommandLineInterface
    {
        private readonly IEvaluationService _evaluationService;
        private readonly ILogger<CommandLineInterface> _logger;

        public CommandLineInterface(IEvaluationService evaluationService, ILogger<CommandLineInterface> logger)
        {
            _evaluationService = evaluationService ?? throw new ArgumentNullException(nameof(evaluationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            DisplayWelcome();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("=== AI Model Evaluator ===");
                    Console.WriteLine("1. List available providers");
                    Console.WriteLine("2. Evaluate single model");
                    Console.WriteLine("3. Compare multiple models");
                    Console.WriteLine("4. View evaluation history");
                    Console.WriteLine("5. Exit");
                    Console.WriteLine();
                    Console.Write("Select an option (1-5): ");

                    var input = Console.ReadLine()?.Trim();
                    
                    switch (input)
                    {
                        case "1":
                            await ListProvidersAsync(cancellationToken);
                            break;
                        case "2":
                            await EvaluateSingleModelAsync(cancellationToken);
                            break;
                        case "3":
                            await CompareMultipleModelsAsync(cancellationToken);
                            break;
                        case "4":
                            await ViewHistoryAsync(cancellationToken);
                            break;
                        case "5":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    _logger.LogError(ex, "Error in main application loop");
                }
            }
        }

        private void DisplayWelcome()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    AI Model Evaluator                       ║");
            Console.WriteLine("║        Cross-platform AI Model Performance Testing          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("This tool helps you evaluate and compare AI models from different providers");
            Console.WriteLine("including OpenAI, Azure, local models, and more.");
            Console.WriteLine();
        }

        private async Task ListProvidersAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Available Providers ===");
            
            var providers = await _evaluationService.GetProvidersAsync();
            var providerList = providers.ToList();
            
            if (!providerList.Any())
            {
                Console.WriteLine("No providers are currently available.");
                Console.WriteLine("Please check your configuration and API keys.");
                return;
            }

            int index = 1;
            foreach (var provider in providerList)
            {
                Console.WriteLine($"{index}. {provider.Name} ({provider.Id})");
                Console.WriteLine($"   Description: {provider.Description}");
                
                try
                {
                    var models = await provider.GetAvailableModelsAsync(cancellationToken);
                    var modelList = models.ToList();
                    
                    if (modelList.Any())
                    {
                        Console.WriteLine($"   Available models: {string.Join(", ", modelList.Take(5))}");
                        if (modelList.Count > 5)
                        {
                            Console.WriteLine($"   ... and {modelList.Count - 5} more");
                        }
                    }
                    else
                    {
                        Console.WriteLine("   No models available");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Error getting models: {ex.Message}");
                    _logger.LogWarning(ex, "Error getting models for provider {ProviderId}", provider.Id);
                }
                
                Console.WriteLine();
                index++;
            }
        }

        private async Task EvaluateSingleModelAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Single Model Evaluation ===");

            var providers = await _evaluationService.GetProvidersAsync();
            var providerList = providers.ToList();
            
            if (!providerList.Any())
            {
                Console.WriteLine("No providers are available. Please check your configuration.");
                return;
            }

            // Select provider
            var selectedProvider = await SelectProviderAsync(providerList, cancellationToken);
            if (selectedProvider == null) return;

            // Select model
            var selectedModel = await SelectModelAsync(selectedProvider, cancellationToken);
            if (selectedModel == null) return;

            // Get prompt
            Console.WriteLine();
            Console.Write("Enter your prompt: ");
            var prompt = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("Prompt cannot be empty.");
                return;
            }

            // Create session and evaluate
            var session = await _evaluationService.StartSessionAsync("Single model evaluation");
            
            Console.WriteLine();
            Console.WriteLine("Evaluating... This may take a moment.");
            Console.WriteLine("Collecting metrics: CPU, Memory, GPU, NPU usage...");
            
            var result = await _evaluationService.EvaluateAsync(
                selectedProvider.Id, selectedModel, prompt, session, cancellationToken);

            await _evaluationService.CompleteSessionAsync(session);

            // Display results
            DisplayEvaluationResult(result);

            // Generate report
            await GenerateAndSaveReportAsync(session, cancellationToken);
        }

        private async Task CompareMultipleModelsAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Multiple Model Comparison ===");

            var providers = await _evaluationService.GetProvidersAsync();
            var providerList = providers.ToList();
            
            if (!providerList.Any())
            {
                Console.WriteLine("No providers are available. Please check your configuration.");
                return;
            }

            // Get prompt first
            Console.Write("Enter your prompt: ");
            var prompt = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("Prompt cannot be empty.");
                return;
            }

            // Select multiple models
            var selectedModels = new List<(IModelProvider provider, string model)>();
            
            Console.WriteLine();
            Console.WriteLine("Select models to compare (enter 'done' when finished):");
            
            while (true)
            {
                Console.WriteLine($"\nCurrently selected: {selectedModels.Count} models");
                if (selectedModels.Any())
                {
                    foreach (var (prov, mod) in selectedModels)
                    {
                        Console.WriteLine($"  - {prov.Name}: {mod}");
                    }
                }
                
                Console.WriteLine();
                var provider = await SelectProviderAsync(providerList, cancellationToken, allowCancel: true);
                if (provider == null) break;
                
                var model = await SelectModelAsync(provider, cancellationToken, allowCancel: true);
                if (model == null) break;
                
                selectedModels.Add((provider, model));
                
                Console.WriteLine($"Added {provider.Name}: {model}");
                Console.Write("Add another model? (y/n): ");
                var continueChoice = Console.ReadLine()?.Trim().ToLower();
                if (continueChoice != "y" && continueChoice != "yes")
                {
                    break;
                }
            }

            if (!selectedModels.Any())
            {
                Console.WriteLine("No models selected for comparison.");
                return;
            }

            // Create session and evaluate all models
            var session = await _evaluationService.StartSessionAsync($"Comparison of {selectedModels.Count} models");
            
            Console.WriteLine();
            Console.WriteLine($"Evaluating {selectedModels.Count} models... This may take several moments.");
            
            for (int i = 0; i < selectedModels.Count; i++)
            {
                var (modelProvider, modelName) = selectedModels[i];
                Console.WriteLine($"[{i + 1}/{selectedModels.Count}] Evaluating {modelProvider.Name}: {modelName}");
                
                var result = await _evaluationService.EvaluateAsync(
                    modelProvider.Id, modelName, prompt, session, cancellationToken);
                
                Console.WriteLine($"   ✓ Completed in {result.Duration.TotalMilliseconds:F0}ms - {(result.IsSuccess ? "Success" : "Failed")}");
            }

            await _evaluationService.CompleteSessionAsync(session);

            // Display comparison results
            DisplayComparisonResults(session);

            // Generate report
            await GenerateAndSaveReportAsync(session, cancellationToken);
        }

        private async Task ViewHistoryAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Evaluation History ===");
            Console.WriteLine("(This feature would show previous evaluation sessions)");
            Console.WriteLine("Implementation: Store sessions in a database or file system");
            
            await Task.Delay(1000, cancellationToken);
        }

        private Task<IModelProvider?> SelectProviderAsync(
            List<IModelProvider> providers, 
            CancellationToken cancellationToken,
            bool allowCancel = false)
        {
            Console.WriteLine();
            Console.WriteLine("Available providers:");
            
            for (int i = 0; i < providers.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {providers[i].Name} ({providers[i].Id})");
            }
            
            if (allowCancel)
            {
                Console.WriteLine("0. Cancel");
            }
            
            Console.Write($"Select a provider (1-{providers.Count}): ");
            
            if (int.TryParse(Console.ReadLine(), out int choice))
            {
                if (allowCancel && choice == 0) return Task.FromResult<IModelProvider?>(null);
                if (choice >= 1 && choice <= providers.Count)
                {
                    return Task.FromResult<IModelProvider?>(providers[choice - 1]);
                }
            }
            
            Console.WriteLine("Invalid selection.");
            return Task.FromResult<IModelProvider?>(null);
        }

        private async Task<string?> SelectModelAsync(
            IModelProvider provider, 
            CancellationToken cancellationToken,
            bool allowCancel = false)
        {
            Console.WriteLine();
            Console.WriteLine($"Getting available models from {provider.Name}...");
            
            try
            {
                var models = await provider.GetAvailableModelsAsync(cancellationToken);
                var modelList = models.ToList();
                
                if (!modelList.Any())
                {
                    Console.WriteLine("No models available from this provider.");
                    return null;
                }
                
                Console.WriteLine("Available models:");
                for (int i = 0; i < modelList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {modelList[i]}");
                }
                
                if (allowCancel)
                {
                    Console.WriteLine("0. Cancel");
                }
                
                Console.Write($"Select a model (1-{modelList.Count}): ");
                
                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    if (allowCancel && choice == 0) return null;
                    if (choice >= 1 && choice <= modelList.Count)
                    {
                        return modelList[choice - 1];
                    }
                }
                
                Console.WriteLine("Invalid selection.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting models: {ex.Message}");
                _logger.LogError(ex, "Error getting models from provider {ProviderId}", provider.Id);
                return null;
            }
        }

        private void DisplayEvaluationResult(EvaluationResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== Evaluation Result ===");
            Console.WriteLine($"Provider: {result.ProviderId}");
            Console.WriteLine($"Model: {result.ModelId}");
            Console.WriteLine($"Status: {(result.IsSuccess ? "✓ Success" : "✗ Failed")}");
            Console.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F0}ms");
            
            if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            
            if (result.IsSuccess)
            {
                Console.WriteLine();
                Console.WriteLine("Response:");
                Console.WriteLine(new string('─', 50));
                Console.WriteLine(result.Response);
                Console.WriteLine(new string('─', 50));
            }

            if (result.Metrics != null)
            {
                Console.WriteLine();
                Console.WriteLine("Performance Metrics:");
                Console.WriteLine($"  CPU Usage:    Avg {result.Metrics.AverageCpuUsage:F1}% | Peak {result.Metrics.PeakCpuUsage:F1}%");
                Console.WriteLine($"  Memory Usage: Avg {result.Metrics.AverageMemoryUsageMB:F0}MB | Peak {result.Metrics.PeakMemoryUsageMB:F0}MB");
                
                if (result.Metrics.AverageGpuUsage > 0)
                {
                    Console.WriteLine($"  GPU Usage:    Avg {result.Metrics.AverageGpuUsage:F1}% | Peak {result.Metrics.PeakGpuUsage:F1}%");
                }
                
                if (result.Metrics.AverageNpuUsage > 0)
                {
                    Console.WriteLine($"  NPU Usage:    Avg {result.Metrics.AverageNpuUsage:F1}% | Peak {result.Metrics.PeakNpuUsage:F1}%");
                }
            }
        }

        private void DisplayComparisonResults(EvaluationSession session)
        {
            Console.WriteLine();
            Console.WriteLine("=== Comparison Results ===");
            Console.WriteLine($"Session: {session.Id}");
            Console.WriteLine($"Total Duration: {session.Duration?.TotalSeconds:F1}s");
            Console.WriteLine();

            var successfulResults = session.Results.Where(r => r.IsSuccess).ToList();
            var failedResults = session.Results.Where(r => !r.IsSuccess).ToList();

            Console.WriteLine($"Results: {successfulResults.Count} successful, {failedResults.Count} failed");
            Console.WriteLine();

            if (successfulResults.Any())
            {
                Console.WriteLine("Performance Summary:");
                Console.WriteLine($"{"Provider",-15} {"Model",-20} {"Duration",-10} {"CPU Avg",-8} {"Memory Avg",-10}");
                Console.WriteLine(new string('─', 70));

                foreach (var result in successfulResults.OrderBy(r => r.Duration))
                {
                    var cpuAvg = result.Metrics?.AverageCpuUsage ?? 0;
                    var memAvg = result.Metrics?.AverageMemoryUsageMB ?? 0;
                    
                    Console.WriteLine($"{result.ProviderId,-15} {result.ModelId,-20} {result.Duration.TotalMilliseconds:F0}ms{"",-4} {cpuAvg:F1}%{"",2} {memAvg:F0}MB");
                }
            }

            if (failedResults.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Failed Evaluations:");
                foreach (var result in failedResults)
                {
                    Console.WriteLine($"  ✗ {result.ProviderId}: {result.ModelId} - {result.ErrorMessage}");
                }
            }
        }

        private async Task GenerateAndSaveReportAsync(EvaluationSession session, CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.Write("Generate HTML report? (y/n): ");
            var generateReport = Console.ReadLine()?.Trim().ToLower();
            
            if (generateReport == "y" || generateReport == "yes")
            {
                try
                {
                    Console.WriteLine("Generating HTML report...");
                    var report = await _evaluationService.GenerateReportAsync(session, "HTML", cancellationToken);
                    
                    // Create reports directory if it doesn't exist
                    var reportsDir = "reports";
                    Directory.CreateDirectory(reportsDir);
                    
                    // Change naming convention: datetime before session ID
                    var fileName = $"evaluation_report_{DateTime.Now:yyyyMMdd_HHmmss}_{session.Id}.html";
                    var filePath = Path.Combine(reportsDir, fileName);
                    await File.WriteAllTextAsync(filePath, report, cancellationToken);
                    
                    Console.WriteLine($"Report saved as: {fileName}");
                    Console.WriteLine($"Full path: {Path.GetFullPath(filePath)}");
                    
                    Console.Write("Open report in browser? (y/n): ");
                    var openReport = Console.ReadLine()?.Trim().ToLower();
                    if (openReport == "y" || openReport == "yes")
                    {
                        try
                        {
                            var fullPath = Path.GetFullPath(filePath);
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = fullPath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not open browser: {ex.Message}");
                            _logger.LogWarning(ex, "Failed to open browser for report");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating report: {ex.Message}");
                    _logger.LogError(ex, "Error generating HTML report");
                }
            }
        }
    }
}
