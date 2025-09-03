using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelEvaluator.Core;
using ModelEvaluator.Metrics;
using ModelEvaluator.Models;
using ModelEvaluator.Providers;
using ModelEvaluator.Reporting;
using ModelEvaluator.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ModelEvaluator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build the host with dependency injection
            var host = CreateHostBuilder(args).Build();

            try
            {
                // Get the command line interface and run the application
                var cli = host.Services.GetRequiredService<CommandLineInterface>();
                await cli.RunAsync();
            }
            catch (Exception ex)
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogCritical(ex, "Application terminated unexpectedly");
                
                Console.WriteLine($"Critical error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Core services
                    services.AddSingleton<IEvaluationService, EvaluationService>();
                    services.AddSingleton<IMetricsCollector, SystemMetricsCollector>();
                    
                    // Model providers
                    services.AddSingleton<IModelProvider>(sp =>
                    {
                        // Try to get OpenAI API key from environment variable
                        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                        if (!string.IsNullOrEmpty(apiKey))
                        {
                            return new OpenAIProvider(apiKey);
                        }
                        
                        // Return a demo provider that explains how to set up OpenAI
                        return new DemoOpenAIProvider();
                    });
                    
                    services.AddSingleton<IModelProvider, AzureAIFoundryLocalProvider>();
                    
                    // Report generators
                    services.AddSingleton<IReportGenerator, HtmlReportGenerator>();
                    
                    // UI
                    services.AddSingleton<CommandLineInterface>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    // Use log levels from configuration instead of hardcoded values
                    // This allows appsettings.json to control the minimum log level
                });
    }
    
    /// <summary>
    /// Demo OpenAI provider that shows how to configure the real one
    /// </summary>
    public class DemoOpenAIProvider : IModelProvider
    {
        public string Id => "openai-demo";
        public string Name => "OpenAI (Demo - API Key Required)";
        public string Description => "OpenAI GPT models - Set OPENAI_API_KEY environment variable to use";

        public Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<string>>(new[] 
            { 
                "gpt-4 (Requires API Key)",
                "gpt-4-turbo (Requires API Key)", 
                "gpt-3.5-turbo (Requires API Key)" 
            });
        }

        public Task<Models.EvaluationResult> EvaluateAsync(string modelId, string prompt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Models.EvaluationResult
            {
                ModelId = modelId,
                ProviderId = Id,
                Prompt = prompt,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = "OpenAI API key not configured. Set the OPENAI_API_KEY environment variable with your API key."
            });
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // Show as available to demonstrate configuration
        }
    }
}
