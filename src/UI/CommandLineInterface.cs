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
                    Console.WriteLine("3. Evaluate model multiple times (with averaged metrics)");
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
                            await EvaluateMultipleTimesAsync(cancellationToken);
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

        private async Task EvaluateMultipleTimesAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Multiple Model Evaluation ===");

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

            // Get number of runs
            Console.WriteLine();
            Console.Write("How many times should the evaluation run? (2-10): ");
            var runsInput = Console.ReadLine();
            
            if (!int.TryParse(runsInput, out int numberOfRuns) || numberOfRuns < 2 || numberOfRuns > 10)
            {
                Console.WriteLine("Invalid number of runs. Please enter a number between 2 and 10.");
                return;
            }

            // Optional delay between runs
            Console.WriteLine();
            Console.Write("Delay between runs in seconds (0-30, default 2): ");
            var delayInput = Console.ReadLine();
            
            if (!int.TryParse(delayInput, out int delaySeconds) || delaySeconds < 0 || delaySeconds > 30)
            {
                delaySeconds = 2; // Default delay
            }

            // Create session and run multiple evaluations
            var session = await _evaluationService.StartSessionAsync($"Multiple evaluation ({numberOfRuns} runs) - {selectedProvider.Name}/{selectedModel}");
            var results = new List<EvaluationResult>();
            
            Console.WriteLine();
            Console.WriteLine($"Running {numberOfRuns} evaluations...");
            Console.WriteLine("Collecting metrics: CPU, Memory, GPU, NPU usage for each run...");
            Console.WriteLine();

            for (int i = 1; i <= numberOfRuns; i++)
            {
                Console.Write($"Run {i}/{numberOfRuns}: ");
                
                try
                {
                    var result = await _evaluationService.EvaluateAsync(
                        selectedProvider.Id, selectedModel, prompt, session, cancellationToken);
                    
                    results.Add(result);
                    
                    if (result.IsSuccess)
                    {
                        Console.WriteLine($"✓ Success ({result.Duration.TotalMilliseconds:F0}ms)");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed ({result.ErrorMessage})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    _logger.LogError(ex, "Error during run {RunNumber}", i);
                }

                // Add delay between runs (except after the last run)
                if (i < numberOfRuns && delaySeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
            }

            await _evaluationService.CompleteSessionAsync(session);

            // Aggregate and display results
            if (results.Any())
            {
                var aggregatedResult = MultipleEvaluationResult.FromResults(results);
                DisplayMultipleEvaluationResults(aggregatedResult);

                // Generate report
                await GenerateAndSaveMultipleReportAsync(session, aggregatedResult, cancellationToken);
            }
            else
            {
                Console.WriteLine("No successful evaluations completed.");
            }
        }

        private async Task ViewHistoryAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.WriteLine("=== Evaluation History ===");
            
            var reportsDir = "reports";
            
            if (!Directory.Exists(reportsDir))
            {
                Console.WriteLine("No reports directory found. No evaluation history available.");
                Console.WriteLine("Run some evaluations first to generate reports.");
                return;
            }

            var reportFiles = Directory.GetFiles(reportsDir, "*.html")
                .Select(file => new FileInfo(file))
                .OrderByDescending(file => file.LastWriteTime)
                .ToList();

            if (!reportFiles.Any())
            {
                Console.WriteLine("No evaluation reports found in the reports directory.");
                Console.WriteLine("Run some evaluations first to generate reports.");
                return;
            }

            Console.WriteLine($"Found {reportFiles.Count} evaluation report(s):");
            Console.WriteLine();
            
            for (int i = 0; i < reportFiles.Count; i++)
            {
                var file = reportFiles[i];
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                var parts = fileName.Split('_');
                
                // Try to parse the filename format: evaluation_report_yyyyMMdd_HHmmss_sessionId
                string displayName = fileName;
                string dateTime = "Unknown";
                string sessionId = "Unknown";
                
                if (parts.Length >= 4 && parts[0] == "evaluation" && parts[1] == "report")
                {
                    var datePart = parts[2];
                    var timePart = parts[3];
                    sessionId = parts.Length > 4 ? string.Join("_", parts.Skip(4)) : "Unknown";
                    
                    if (DateTime.TryParseExact($"{datePart}_{timePart}", "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var parsedDateTime))
                    {
                        dateTime = parsedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
                
                // Extract provider and model info from the HTML file
                var (provider, model, prompt) = await ExtractReportInfoAsync(file.FullName);
                
                Console.WriteLine($"{i + 1,2}. {dateTime} - Session: {sessionId}");
                Console.WriteLine($"     Provider: {provider} | Model: {model}");
                if (!string.IsNullOrEmpty(prompt))
                {
                    var truncatedPrompt = prompt.Length > 50 ? prompt.Substring(0, 50) + "..." : prompt;
                    Console.WriteLine($"     Prompt: {truncatedPrompt}");
                }
                Console.WriteLine($"     File: {file.Name} ({file.Length / 1024:F1} KB)");
                Console.WriteLine($"     Modified: {file.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }

            Console.WriteLine("Options:");
            Console.WriteLine("  Enter a number (1-{0}) to open a report", reportFiles.Count);
            Console.WriteLine("  Enter 'delete' to delete a report");
            Console.WriteLine("  Enter 'clear' to delete all reports");
            Console.WriteLine("  Press Enter to return to main menu");
            Console.WriteLine();
            Console.Write("Your choice: ");
            
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                return; // Return to main menu
            }
            
            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Are you sure you want to delete ALL reports? (y/N): ");
                var confirm = Console.ReadLine()?.Trim().ToLower();
                if (confirm == "y" || confirm == "yes")
                {
                    try
                    {
                        foreach (var file in reportFiles)
                        {
                            file.Delete();
                        }
                        Console.WriteLine($"Deleted {reportFiles.Count} report(s).");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting reports: {ex.Message}");
                        _logger.LogError(ex, "Error deleting all reports");
                    }
                }
                else
                {
                    Console.WriteLine("Operation cancelled.");
                }
                return;
            }
            
            if (input.Equals("delete", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write($"Enter report number to delete (1-{reportFiles.Count}): ");
                var deleteInput = Console.ReadLine()?.Trim();
                
                if (int.TryParse(deleteInput, out int deleteChoice) && deleteChoice >= 1 && deleteChoice <= reportFiles.Count)
                {
                    var fileToDelete = reportFiles[deleteChoice - 1];
                    Console.Write($"Delete '{fileToDelete.Name}'? (y/N): ");
                    var confirm = Console.ReadLine()?.Trim().ToLower();
                    
                    if (confirm == "y" || confirm == "yes")
                    {
                        try
                        {
                            fileToDelete.Delete();
                            Console.WriteLine($"Deleted '{fileToDelete.Name}'.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting report: {ex.Message}");
                            _logger.LogError(ex, "Error deleting report {FileName}", fileToDelete.Name);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Delete cancelled.");
                    }
                }
                else
                {
                    Console.WriteLine("Invalid report number.");
                }
                return;
            }
            
            // Try to parse as report number to open
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= reportFiles.Count)
            {
                var selectedFile = reportFiles[choice - 1];
                
                try
                {
                    Console.WriteLine($"Opening '{selectedFile.Name}' in your default browser...");
                    
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = selectedFile.FullName,
                        UseShellExecute = true
                    };
                    
                    System.Diagnostics.Process.Start(startInfo);
                    Console.WriteLine("Report opened successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not open report: {ex.Message}");
                    Console.WriteLine($"You can manually open: {selectedFile.FullName}");
                    _logger.LogWarning(ex, "Failed to open report {FileName}", selectedFile.Name);
                }
            }
            else
            {
                Console.WriteLine("Invalid choice. Please enter a number, 'delete', 'clear', or press Enter.");
            }
            
            await Task.Delay(100, cancellationToken); // Small delay for responsiveness
        }

        private async Task<(string provider, string model, string prompt)> ExtractReportInfoAsync(string filePath)
        {
            try
            {
                var htmlContent = await File.ReadAllTextAsync(filePath);
                
                string provider = "Unknown";
                string model = "Unknown";
                string prompt = "";
                
                // Extract from table row - looking for the pattern in the results table
                var tableRowMatch = System.Text.RegularExpressions.Regex.Match(
                    htmlContent, 
                    @"<tr>\s*<td>([^<]+)</td>\s*<td>([^<]+)</td>\s*<td[^>]*>([^<]+)</td>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                
                if (tableRowMatch.Success)
                {
                    provider = tableRowMatch.Groups[1].Value.Trim();
                    model = tableRowMatch.Groups[2].Value.Trim();
                    var promptValue = tableRowMatch.Groups[3].Value.Trim();
                    
                    // Decode HTML entities in prompt
                    prompt = System.Net.WebUtility.HtmlDecode(promptValue);
                }
                else
                {
                    // Fallback: try to find provider and model in metrics section
                    var metricsMatch = System.Text.RegularExpressions.Regex.Match(
                        htmlContent, 
                        @"<h3>([^-]+)-\s*([^<]+)</h3>", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (metricsMatch.Success)
                    {
                        provider = metricsMatch.Groups[1].Value.Trim();
                        model = metricsMatch.Groups[2].Value.Trim();
                    }
                }
                
                return (provider, model, prompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract report info from {FilePath}", filePath);
                return ("Unknown", "Unknown", "");
            }
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

        private void DisplayMultipleEvaluationResults(MultipleEvaluationResult result)
        {
            Console.WriteLine();
            Console.WriteLine("=== Multiple Evaluation Results ===");
            Console.WriteLine($"Provider: {result.ProviderId}");
            Console.WriteLine($"Model: {result.ModelId}");
            Console.WriteLine($"Prompt: {result.Prompt}");
            Console.WriteLine();
            
            // Summary statistics
            Console.WriteLine("Execution Summary:");
            Console.WriteLine($"  Total runs:      {result.TotalRuns}");
            Console.WriteLine($"  Successful runs: {result.SuccessfulRuns} ({result.SuccessRate:F1}%)");
            Console.WriteLine($"  Failed runs:     {result.FailedRuns}");
            Console.WriteLine();
            
            // Timing statistics
            Console.WriteLine("Timing Statistics:");
            Console.WriteLine($"  Average duration: {result.AverageDuration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  Min duration:     {result.MinDuration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  Max duration:     {result.MaxDuration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"  Total duration:   {result.TotalDuration.TotalSeconds:F1}s");
            Console.WriteLine();

            // Response analysis
            Console.WriteLine("Response Analysis:");
            Console.WriteLine($"  Unique responses: {result.UniqueResponses.Count}");
            Console.WriteLine($"  Consistent responses: {(result.AllResponsesIdentical ? "Yes" : "No")}");
            
            if (result.UniqueResponses.Count > 1 && result.UniqueResponses.Count <= 3)
            {
                Console.WriteLine("  Response variations:");
                foreach (var kvp in result.ResponseFrequency.OrderByDescending(x => x.Value))
                {
                    var truncatedResponse = kvp.Key.Length > 100 ? kvp.Key.Substring(0, 100) + "..." : kvp.Key;
                    Console.WriteLine($"    {kvp.Value}x: {truncatedResponse}");
                }
            }
            Console.WriteLine();

            // Performance metrics
            if (result.AggregatedMetrics != null)
            {
                var metrics = result.AggregatedMetrics;
                Console.WriteLine("Aggregated Performance Metrics:");
                Console.WriteLine($"  CPU Usage:");
                Console.WriteLine($"    Average: {metrics.AverageCpuUsage:F1}% (range: {metrics.MinCpuUsage:F1}% - {metrics.MaxCpuUsage:F1}%)");
                Console.WriteLine($"    Peak:    {metrics.PeakCpuUsage:F1}%");
                
                Console.WriteLine($"  Memory Usage:");
                Console.WriteLine($"    Average: {metrics.AverageMemoryUsageMB:F0}MB (range: {metrics.MinMemoryUsageMB:F0}MB - {metrics.MaxMemoryUsageMB:F0}MB)");
                Console.WriteLine($"    Peak:    {metrics.PeakMemoryUsageMB:F0}MB");
                
                if (metrics.AverageGpuUsage > 0)
                {
                    Console.WriteLine($"  GPU Usage:");
                    Console.WriteLine($"    Average: {metrics.AverageGpuUsage:F1}% (range: {metrics.MinGpuUsage:F1}% - {metrics.MaxGpuUsage:F1}%)");
                    Console.WriteLine($"    Peak:    {metrics.PeakGpuUsage:F1}%");
                }
                
                if (metrics.AverageNpuUsage > 0)
                {
                    Console.WriteLine($"  NPU Usage:");
                    Console.WriteLine($"    Average: {metrics.AverageNpuUsage:F1}% (range: {metrics.MinNpuUsage:F1}% - {metrics.MaxNpuUsage:F1}%)");
                    Console.WriteLine($"    Peak:    {metrics.PeakNpuUsage:F1}%");
                }
            }

            // Individual run details (if requested)
            Console.WriteLine();
            Console.Write("Show individual run details? (y/n): ");
            var showDetails = Console.ReadLine()?.Trim().ToLower();
            if (showDetails == "y" || showDetails == "yes")
            {
                Console.WriteLine();
                Console.WriteLine("Individual Run Details:");
                for (int i = 0; i < result.IndividualResults.Count; i++)
                {
                    var run = result.IndividualResults[i];
                    Console.WriteLine($"  Run {i + 1}: {(run.IsSuccess ? "✓" : "✗")} {run.Duration.TotalMilliseconds:F0}ms");
                    if (!run.IsSuccess && !string.IsNullOrEmpty(run.ErrorMessage))
                    {
                        Console.WriteLine($"    Error: {run.ErrorMessage}");
                    }
                }
            }
        }

        private async Task GenerateAndSaveMultipleReportAsync(EvaluationSession session, MultipleEvaluationResult result, CancellationToken cancellationToken)
        {
            Console.WriteLine();
            Console.Write("Generate HTML report for multiple evaluation? (y/n): ");
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
                    
                    // Change naming convention: datetime before session ID with "multiple" indicator
                    var fileName = $"evaluation_report_multiple_{DateTime.Now:yyyyMMdd_HHmmss}_{session.Id}.html";
                    var filePath = Path.Combine(reportsDir, fileName);
                    await File.WriteAllTextAsync(filePath, report, cancellationToken);
                    
                    Console.WriteLine($"Report saved as: {fileName}");
                    Console.WriteLine($"Full path: {Path.GetFullPath(filePath)}");
                    Console.WriteLine($"Report includes data from {result.TotalRuns} evaluation runs with aggregated metrics.");
                    
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
