using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AI.Foundry.Local;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using ModelEvaluator.Core;
using ModelEvaluator.Models;

namespace ModelEvaluator.Providers
{
    /// <summary>
    /// Azure AI Foundry Local provider for running local models via Azure AI Foundry Local SDK
    /// </summary>
    public class AzureAIFoundryLocalProvider : IModelProvider, IDisposable
    {
        private readonly Dictionary<string, FoundryLocalManager> _managers = new();
        private readonly Dictionary<string, ChatClient> _chatClients = new();
        private readonly string[] _availableModels = new[]
        {
            "phi-4",
            "mistral-7b-v0.2", 
            "phi-3.5-mini",
            "phi-3-mini-128k",
            "phi-3-mini-4k",
            "deepseek-r1-14b",
            "deepseek-r1-7b",
            "qwen2.5-0.5b",
            "qwen2.5-1.5b",
            "qwen2.5-coder-0.5b",
            "qwen2.5-coder-7b",
            "qwen2.5-coder-1.5b",
            "phi-4-mini",
            "phi-4-mini-reasoning",
            "qwen2.5-14b",
            "qwen2.5-7b",
            "qwen2.5-coder-14b"
        };

        public string Id => "azure-foundry-local";
        public string Name => "Azure AI Foundry Local";
        public string Description => "Local AI models via Azure AI Foundry Local SDK";

        public async Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to get the live list from foundry command
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "foundry",
                    Arguments = "model list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Parse the output to extract model aliases
                    var models = new HashSet<string>(); // Use HashSet to avoid duplicates
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // Skip header, separator lines, and empty lines
                        if (string.IsNullOrWhiteSpace(trimmedLine) ||
                            trimmedLine.Contains("Alias") || 
                            trimmedLine.Contains("---") ||
                            trimmedLine.StartsWith("CPU") || 
                            trimmedLine.StartsWith("GPU"))
                            continue;

                        // Look for lines that start with a letter (model aliases)
                        if (char.IsLetter(trimmedLine[0]))
                        {
                            var parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                var alias = parts[0].Trim();
                                // Make sure it's a valid model alias and not "Alias" header
                                if (!string.IsNullOrEmpty(alias) && 
                                    !alias.Equals("Alias", StringComparison.OrdinalIgnoreCase) &&
                                    alias.Length > 2)
                                {
                                    models.Add(alias);
                                }
                            }
                        }
                    }

                    if (models.Count > 0)
                    {
                        return models.OrderBy(m => m).ToArray();
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to static list if foundry command fails
            }

            // Return static list as fallback
            await Task.Delay(50, cancellationToken);
            return _availableModels;
        }

        public async Task<EvaluationResult> EvaluateAsync(string modelId, string prompt, CancellationToken cancellationToken = default)
        {
            var result = new EvaluationResult
            {
                ModelId = modelId,
                ProviderId = Id,
                Prompt = prompt,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Get or create the manager and chat client for this model
                var chatClient = await GetChatClientAsync(modelId, cancellationToken);

                // Create chat completion request
                var messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage(prompt)
                };

                var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

                result.Response = completion.Value.Content[0].Text;
                result.IsSuccess = true;

                // Add metadata
                result.Metadata["model_id"] = modelId;
                result.Metadata["provider"] = "azure-foundry-local";
                result.Metadata["completion_tokens"] = completion.Value.Usage?.OutputTokenCount ?? 0;
                result.Metadata["prompt_tokens"] = completion.Value.Usage?.InputTokenCount ?? 0;
                result.Metadata["total_tokens"] = completion.Value.Usage?.TotalTokenCount ?? 0;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Azure AI Foundry Local error: {ex.Message}";
                
                // Include inner exception details if available
                if (ex.InnerException != null)
                {
                    result.ErrorMessage += $" Inner: {ex.InnerException.Message}";
                }
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to check if Foundry Local service is available
                // We'll attempt to start a lightweight model to verify connectivity
                await Task.Delay(100, cancellationToken);
                
                // In a real implementation, you might ping the service or check for installation
                // For now, we'll assume it's available if the SDK is installed
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<ChatClient> GetChatClientAsync(string modelId, CancellationToken cancellationToken = default)
        {
            // Return existing client if already created
            if (_chatClients.TryGetValue(modelId, out var existingClient))
            {
                return existingClient;
            }

            // Create new manager for this model
            var manager = await FoundryLocalManager.StartModelAsync(aliasOrModelId: modelId);

            _managers[modelId] = manager;

            // Get model info
            var modelInfo = await manager.GetModelInfoAsync(aliasOrModelId: modelId);

            // Create OpenAI client
            var apiKey = new ApiKeyCredential(manager.ApiKey);
            var openAIClient = new OpenAIClient(apiKey, new OpenAIClientOptions
            {
                Endpoint = manager.Endpoint
            });

            // Create chat client
            var chatClient = openAIClient.GetChatClient(modelInfo?.ModelId);
            _chatClients[modelId] = chatClient;

            return chatClient;
        }

        public void Dispose()
        {
            // Dispose all managers
            foreach (var manager in _managers.Values)
            {
                try
                {
                    manager?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }

            _managers.Clear();
            _chatClients.Clear();
        }
    }
}
