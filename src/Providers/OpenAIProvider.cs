using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Core;
using ModelEvaluator.Models;
using Newtonsoft.Json;

namespace ModelEvaluator.Providers
{
    /// <summary>
    /// OpenAI API provider for model evaluation
    /// </summary>
    public class OpenAIProvider : IModelProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public string Id => "openai";
        public string Name => "OpenAI";
        public string Description => "OpenAI GPT models via API";

        public OpenAIProvider(string apiKey, string baseUrl = "https://api.openai.com/v1")
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var modelsResponse = JsonConvert.DeserializeObject<OpenAIModelsResponse>(content);
                    
                    var modelIds = new List<string>();
                    if (modelsResponse?.Data != null)
                    {
                        foreach (var model in modelsResponse.Data)
                        {
                            if (model.Id != null && model.Id.StartsWith("gpt"))
                            {
                                modelIds.Add(model.Id);
                            }
                        }
                    }
                    
                    return modelIds;
                }
                
                // Fallback to common models if API call fails
                return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
            }
            catch
            {
                // Fallback to common models if API call fails
                return new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" };
            }
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
                var requestBody = new
                {
                    model = modelId,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var openAIResponse = JsonConvert.DeserializeObject<OpenAIChatResponse>(responseContent);
                    
                    result.Response = openAIResponse?.Choices?[0]?.Message?.Content ?? "No response";
                    result.IsSuccess = true;
                    
                    // Add metadata
                    if (openAIResponse?.Usage != null)
                    {
                        result.Metadata["prompt_tokens"] = openAIResponse.Usage.PromptTokens;
                        result.Metadata["completion_tokens"] = openAIResponse.Usage.CompletionTokens;
                        result.Metadata["total_tokens"] = openAIResponse.Usage.TotalTokens;
                    }
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"API Error: {response.StatusCode} - {responseContent}";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
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
                var response = await _httpClient.GetAsync($"{_baseUrl}/models", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        // OpenAI API response models
        private class OpenAIModelsResponse
        {
            [JsonProperty("data")]
            public List<OpenAIModel>? Data { get; set; }
        }

        private class OpenAIModel
        {
            [JsonProperty("id")]
            public string? Id { get; set; }
        }

        private class OpenAIChatResponse
        {
            [JsonProperty("choices")]
            public List<OpenAIChoice>? Choices { get; set; }

            [JsonProperty("usage")]
            public OpenAIUsage? Usage { get; set; }
        }

        private class OpenAIChoice
        {
            [JsonProperty("message")]
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            [JsonProperty("content")]
            public string? Content { get; set; }
        }

        private class OpenAIUsage
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonProperty("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}
