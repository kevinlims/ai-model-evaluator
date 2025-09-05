using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Core;
using ModelEvaluator.Models;

namespace ModelEvaluator.Reporting
{
    /// <summary>
    /// Generates HTML reports for evaluation sessions
    /// </summary>
    public class HtmlReportGenerator : IReportGenerator
    {
        public string Format => "HTML";
        public string FileExtension => ".html";

        public async Task<string> GenerateReportAsync(EvaluationSession session, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => GenerateHtmlReport(session), cancellationToken);
        }

        private string GenerateHtmlReport(EvaluationSession session)
        {
            var html = new StringBuilder();
            
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"UTF-8\">");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            html.AppendLine("    <title>AI Model Evaluation Report</title>");
            html.AppendLine("    <style>");
            html.AppendLine(GetCssStyles());
            html.AppendLine("    </style>");
            html.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            
            // Header
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine("        <header>");
            html.AppendLine("            <h1>🚀 AI Model Evaluation Report</h1>");
            html.AppendLine($"           <p class=\"session-info\">Session ID: {session.Id}</p>");
            html.AppendLine($"           <p class=\"session-info\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            if (!string.IsNullOrEmpty(session.Description))
            {
                html.AppendLine($"           <p class=\"session-info\">Description: {session.Description}</p>");
            }
            html.AppendLine("        </header>");

            // Quick Summary Cards (Critical Overview)
            GenerateQuickSummarySection(html, session);
            
            // Results (Most Critical - moved to top)
            GenerateResultsSection(html, session);
            
            // Performance Charts (Visual insights)
            GenerateChartsSection(html, session);
            
            // Detailed Metrics
            GenerateMetricsSection(html, session);
            
            // System Information (moved to bottom)
            GenerateDeviceMetadataSection(html, session);

            html.AppendLine("    </div>");
            html.AppendLine("    <script>");
            html.AppendLine(GetJavaScript(session));
            html.AppendLine("    </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private void GenerateQuickSummarySection(StringBuilder html, EvaluationSession session)
        {
            var successfulResults = session.Results.Where(r => r.IsSuccess).ToList();
            var failedResults = session.Results.Where(r => !r.IsSuccess).ToList();
            var successRate = session.Results.Count > 0 ? (double)successfulResults.Count / session.Results.Count * 100 : 0;
            
            html.AppendLine("        <section class=\"quick-summary\">");
            html.AppendLine("            <div class=\"summary-cards\">");
            
            // Success Rate Card
            var successClass = successRate >= 80 ? "excellent" : successRate >= 60 ? "good" : "poor";
            html.AppendLine($"               <div class=\"summary-card {successClass}\">");
            html.AppendLine("                    <div class=\"card-icon\">📊</div>");
            html.AppendLine($"                   <div class=\"card-number\">{successRate:F1}%</div>");
            html.AppendLine($"                   <div class=\"card-label\">Success Rate</div>");
            html.AppendLine($"                   <div class=\"card-detail\">{successfulResults.Count}/{session.Results.Count} evaluations</div>");
            html.AppendLine($"               </div>");
            
            // Average Response Time Card
            if (successfulResults.Count > 0)
            {
                var avgDuration = successfulResults.Average(r => r.Duration.TotalMilliseconds);
                var timeClass = avgDuration <= 1000 ? "excellent" : avgDuration <= 3000 ? "good" : "poor";
                html.AppendLine($"               <div class=\"summary-card {timeClass}\">");
                html.AppendLine("                    <div class=\"card-icon\">⚡</div>");
                html.AppendLine($"                   <div class=\"card-number\">{avgDuration:F0}ms</div>");
                html.AppendLine($"                   <div class=\"card-label\">Avg Response Time</div>");
                html.AppendLine($"                   <div class=\"card-detail\">Across {successfulResults.Count} successful runs</div>");
                html.AppendLine($"               </div>");
            }
            
            // Total Evaluations Card
            html.AppendLine($"               <div class=\"summary-card info\">");
            html.AppendLine("                    <div class=\"card-icon\">🔍</div>");
            html.AppendLine($"                   <div class=\"card-number\">{session.Results.Count}</div>");
            html.AppendLine($"                   <div class=\"card-label\">Total Evaluations</div>");
            if (session.Duration.HasValue)
            {
                html.AppendLine($"                   <div class=\"card-detail\">Completed in {session.Duration.Value.TotalSeconds:F1}s</div>");
            }
            html.AppendLine($"               </div>");
            
            // Models Tested Card
            var uniqueModels = session.Results.Select(r => $"{r.ProviderId}/{r.ModelId}").Distinct().Count();
            html.AppendLine($"               <div class=\"summary-card info\">");
            html.AppendLine("                    <div class=\"card-icon\">🤖</div>");
            html.AppendLine($"                   <div class=\"card-number\">{uniqueModels}</div>");
            html.AppendLine($"                   <div class=\"card-label\">Models Tested</div>");
            var uniqueProviders = session.Results.Select(r => r.ProviderId).Distinct().Count();
            html.AppendLine($"                   <div class=\"card-detail\">{uniqueProviders} provider{(uniqueProviders == 1 ? "" : "s")}</div>");
            html.AppendLine($"               </div>");
            
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
        }

        private void GenerateDetailedSummarySection(StringBuilder html, EvaluationSession session)
        {
            var successfulResults = session.Results.Where(r => r.IsSuccess).ToList();
            var failedResults = session.Results.Where(r => !r.IsSuccess).ToList();
            
            html.AppendLine("        <section class=\"summary\">");
            html.AppendLine("            <h2>Summary</h2>");
            html.AppendLine("            <div class=\"summary-grid\">");
            html.AppendLine($"               <div class=\"summary-item\">");
            html.AppendLine($"                   <div class=\"summary-number\">{session.Results.Count}</div>");
            html.AppendLine($"                   <div class=\"summary-label\">Total Evaluations</div>");
            html.AppendLine($"               </div>");
            html.AppendLine($"               <div class=\"summary-item\">");
            html.AppendLine($"                   <div class=\"summary-number\">{successfulResults.Count}</div>");
            html.AppendLine($"                   <div class=\"summary-label\">Successful</div>");
            html.AppendLine($"               </div>");
            html.AppendLine($"               <div class=\"summary-item\">");
            html.AppendLine($"                   <div class=\"summary-number\">{failedResults.Count}</div>");
            html.AppendLine($"                   <div class=\"summary-label\">Failed</div>");
            html.AppendLine($"               </div>");
            
            if (session.Duration.HasValue)
            {
                html.AppendLine($"               <div class=\"summary-item\">");
                html.AppendLine($"                   <div class=\"summary-number\">{session.Duration.Value.TotalSeconds:F1}s</div>");
                html.AppendLine($"                   <div class=\"summary-label\">Total Duration</div>");
                html.AppendLine($"               </div>");
            }
            
            if (successfulResults.Count > 0)
            {
                var avgDuration = successfulResults.Average(r => r.Duration.TotalMilliseconds);
                html.AppendLine($"               <div class=\"summary-item\">");
                html.AppendLine($"                   <div class=\"summary-number\">{avgDuration:F0}ms</div>");
                html.AppendLine($"                   <div class=\"summary-label\">Avg Response Time</div>");
                html.AppendLine($"               </div>");
            }
            
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
        }

        private void GenerateDeviceMetadataSection(StringBuilder html, EvaluationSession session)
        {
            if (session.DeviceInfo == null) return;

            var device = session.DeviceInfo;
            
            html.AppendLine("        <section class=\"device-metadata\">");
            html.AppendLine("            <h2>System Information</h2>");
            html.AppendLine("            <div class=\"device-info\">");
            
            // Operating System Information
            html.AppendLine("                <div class=\"info-group\">");
            html.AppendLine("                    <h3>Operating System</h3>");
            html.AppendLine("                    <div class=\"info-grid\">");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">OS:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.OperatingSystem)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Version:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.OSVersion)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Architecture:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.OSArchitecture)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Machine:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.MachineName)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine("                    </div>");
            html.AppendLine("                </div>");

            // Processor Information
            html.AppendLine("                <div class=\"info-group\">");
            html.AppendLine("                    <h3>Processor</h3>");
            html.AppendLine("                    <div class=\"info-grid\">");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Name:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.ProcessorName)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Architecture:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.ProcessorArchitecture)}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Physical Cores:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{device.ProcessorCores}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Logical Processors:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{device.LogicalProcessors}</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine("                    </div>");
            html.AppendLine("                </div>");

            // Memory Information
            html.AppendLine("                <div class=\"info-group\">");
            html.AppendLine("                    <h3>Memory</h3>");
            html.AppendLine("                    <div class=\"info-grid\">");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Total Memory:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{device.TotalMemoryMB:N0} MB ({device.TotalMemoryMB / 1024.0:F1} GB)</span>");
            html.AppendLine($"                       </div>");
            if (device.AvailableMemoryMB > 0)
            {
                html.AppendLine($"                       <div class=\"info-item\">");
                html.AppendLine($"                           <span class=\"info-label\">Available Memory:</span>");
                html.AppendLine($"                           <span class=\"info-value\">{device.AvailableMemoryMB:N0} MB ({device.AvailableMemoryMB / 1024.0:F1} GB)</span>");
                html.AppendLine($"                       </div>");
            }
            html.AppendLine("                    </div>");
            html.AppendLine("                </div>");

            // GPU Information
            if (device.GpuDevices.Any())
            {
                html.AppendLine("                <div class=\"info-group\">");
                html.AppendLine("                    <h3>Graphics Devices</h3>");
                html.AppendLine("                    <ul class=\"gpu-list\">");
                foreach (var gpu in device.GpuDevices)
                {
                    html.AppendLine($"                       <li>{System.Web.HttpUtility.HtmlEncode(gpu)}</li>");
                }
                html.AppendLine("                    </ul>");
                html.AppendLine("                </div>");
            }

            // Runtime Information
            html.AppendLine("                <div class=\"info-group\">");
            html.AppendLine("                    <h3>Runtime Environment</h3>");
            html.AppendLine("                    <div class=\"info-grid\">");
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">.NET Version:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.DotNetVersion)}</span>");
            html.AppendLine($"                       </div>");
            
            if (device.AdditionalInfo.ContainsKey("FrameworkDescription"))
            {
                html.AppendLine($"                       <div class=\"info-item\">");
                html.AppendLine($"                           <span class=\"info-label\">Framework:</span>");
                html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.AdditionalInfo["FrameworkDescription"])}</span>");
                html.AppendLine($"                       </div>");
            }
            
            if (device.AdditionalInfo.ContainsKey("RuntimeIdentifier"))
            {
                html.AppendLine($"                       <div class=\"info-item\">");
                html.AppendLine($"                           <span class=\"info-label\">Runtime ID:</span>");
                html.AppendLine($"                           <span class=\"info-value\">{System.Web.HttpUtility.HtmlEncode(device.AdditionalInfo["RuntimeIdentifier"])}</span>");
                html.AppendLine($"                       </div>");
            }
            
            html.AppendLine($"                       <div class=\"info-item\">");
            html.AppendLine($"                           <span class=\"info-label\">Data Collected:</span>");
            html.AppendLine($"                           <span class=\"info-value\">{device.CollectedAt:yyyy-MM-dd HH:mm:ss} UTC</span>");
            html.AppendLine($"                       </div>");
            html.AppendLine("                    </div>");
            html.AppendLine("                </div>");

            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
        }

        private void GenerateResultsSection(StringBuilder html, EvaluationSession session)
        {
            html.AppendLine("        <section class=\"results-main\">");
            html.AppendLine("            <h2>📋 Evaluation Results</h2>");
            
            // Results by Status
            var successfulResults = session.Results.Where(r => r.IsSuccess).ToList();
            var failedResults = session.Results.Where(r => !r.IsSuccess).ToList();
            
            if (failedResults.Any())
            {
                html.AppendLine("            <div class=\"results-status-tabs\">");
                html.AppendLine("                <button class=\"tab-button active\" onclick=\"showTab('all')\">All Results</button>");
                html.AppendLine("                <button class=\"tab-button\" onclick=\"showTab('successful')\">✅ Successful</button>");
                html.AppendLine("                <button class=\"tab-button\" onclick=\"showTab('failed')\">❌ Failed</button>");
                html.AppendLine("            </div>");
            }
            
            html.AppendLine("            <div id=\"all-results\" class=\"results-table\">");
            GenerateResultsTable(html, session.Results, "All Evaluation Results");
            html.AppendLine("            </div>");
            
            if (failedResults.Any())
            {
                html.AppendLine("            <div id=\"successful-results\" class=\"results-table\" style=\"display: none;\">");
                GenerateResultsTable(html, successfulResults, "Successful Evaluations");
                html.AppendLine("            </div>");
                
                html.AppendLine("            <div id=\"failed-results\" class=\"results-table\" style=\"display: none;\">");
                GenerateFailedResultsTable(html, failedResults);
                html.AppendLine("            </div>");
            }
            
            html.AppendLine("        </section>");
        }

        private void GenerateResultsTable(StringBuilder html, IEnumerable<EvaluationResult> results, string title)
        {
            html.AppendLine($"               <h3>{title}</h3>");
            html.AppendLine("                <table>");
            html.AppendLine("                    <thead>");
            html.AppendLine("                        <tr>");
            html.AppendLine("                            <th>Status</th>");
            html.AppendLine("                            <th>Provider</th>");
            html.AppendLine("                            <th>Model</th>");
            html.AppendLine("                            <th>Duration</th>");
            html.AppendLine("                            <th>Prompt</th>");
            html.AppendLine("                            <th>Response</th>");
            html.AppendLine("                            <th>Performance</th>");
            html.AppendLine("                        </tr>");
            html.AppendLine("                    </thead>");
            html.AppendLine("                    <tbody>");

            foreach (var result in results.OrderBy(r => r.StartTime))
            {
                var statusClass = result.IsSuccess ? "success" : "error";
                var statusIcon = result.IsSuccess ? "✅" : "❌";
                var truncatedPrompt = result.Prompt.Length > 80 ? result.Prompt.Substring(0, 80) + "..." : result.Prompt;
                var truncatedResponse = result.Response.Length > 120 ? result.Response.Substring(0, 120) + "..." : result.Response;
                
                // Extract model information from metadata for Azure AI Foundry Local provider
                var modelDisplay = result.ModelId;
                if (result.ProviderId == "azure-foundry-local" && result.Metadata != null)
                {
                    var foundryId = result.Metadata.TryGetValue("foundry_model_id", out var id) ? id?.ToString() : null;
                    var foundryAlias = result.Metadata.TryGetValue("model_alias", out var alias) ? alias?.ToString() : null;
                    
                    if (!string.IsNullOrEmpty(foundryId) && foundryId != result.ModelId)
                    {
                        modelDisplay = $"{result.ModelId}<br/><small class=\"model-detail\">({foundryId})</small>";
                    }
                    else if (!string.IsNullOrEmpty(foundryAlias) && foundryAlias != result.ModelId)
                    {
                        modelDisplay = $"{result.ModelId}<br/><small class=\"model-detail\">({foundryAlias})</small>";
                    }
                }
                
                // Performance indicators
                var performanceClass = "neutral";
                var performanceText = "Normal";
                var duration = result.Duration.TotalMilliseconds;
                
                if (result.IsSuccess)
                {
                    if (duration <= 1000)
                    {
                        performanceClass = "excellent";
                        performanceText = "🚀 Fast";
                    }
                    else if (duration <= 3000)
                    {
                        performanceClass = "good";
                        performanceText = "⚡ Good";
                    }
                    else if (duration <= 10000)
                    {
                        performanceClass = "warning";
                        performanceText = "⏳ Slow";
                    }
                    else
                    {
                        performanceClass = "poor";
                        performanceText = "🐌 Very Slow";
                    }
                }
                
                html.AppendLine("                        <tr>");
                html.AppendLine($"                           <td class=\"{statusClass}\">{statusIcon}</td>");
                html.AppendLine($"                           <td><span class=\"provider-badge\">{result.ProviderId}</span></td>");
                html.AppendLine($"                           <td class=\"model-cell\"><strong>{modelDisplay}</strong></td>");
                html.AppendLine($"                           <td><span class=\"duration\">{result.Duration.TotalMilliseconds:F0}ms</span></td>");
                html.AppendLine($"                           <td class=\"expandable\" title=\"{System.Web.HttpUtility.HtmlEncode(result.Prompt)}\">{System.Web.HttpUtility.HtmlEncode(truncatedPrompt)}</td>");
                html.AppendLine($"                           <td class=\"expandable\" title=\"{System.Web.HttpUtility.HtmlEncode(result.Response)}\">{System.Web.HttpUtility.HtmlEncode(truncatedResponse)}</td>");
                html.AppendLine($"                           <td class=\"performance {performanceClass}\">{performanceText}</td>");
                html.AppendLine("                        </tr>");
            }

            html.AppendLine("                    </tbody>");
            html.AppendLine("                </table>");
        }

        private void GenerateFailedResultsTable(StringBuilder html, IEnumerable<EvaluationResult> failedResults)
        {
            html.AppendLine("                <h3>❌ Failed Evaluations - Detailed Error Information</h3>");
            html.AppendLine("                <table>");
            html.AppendLine("                    <thead>");
            html.AppendLine("                        <tr>");
            html.AppendLine("                            <th>Provider</th>");
            html.AppendLine("                            <th>Model</th>");
            html.AppendLine("                            <th>Error Message</th>");
            html.AppendLine("                            <th>Duration</th>");
            html.AppendLine("                            <th>Prompt</th>");
            html.AppendLine("                        </tr>");
            html.AppendLine("                    </thead>");
            html.AppendLine("                    <tbody>");

            foreach (var result in failedResults.OrderBy(r => r.StartTime))
            {
                var truncatedPrompt = result.Prompt.Length > 100 ? result.Prompt.Substring(0, 100) + "..." : result.Prompt;
                var errorMessage = string.IsNullOrEmpty(result.ErrorMessage) ? "Unknown error" : result.ErrorMessage;
                
                // Extract model information from metadata for Azure AI Foundry Local provider
                var modelDisplay = result.ModelId;
                if (result.ProviderId == "azure-foundry-local" && result.Metadata != null)
                {
                    var foundryId = result.Metadata.TryGetValue("foundry_model_id", out var id) ? id?.ToString() : null;
                    var foundryAlias = result.Metadata.TryGetValue("model_alias", out var alias) ? alias?.ToString() : null;
                    
                    if (!string.IsNullOrEmpty(foundryId) && foundryId != result.ModelId)
                    {
                        modelDisplay = $"{result.ModelId}<br/><small class=\"model-detail\">({foundryId})</small>";
                    }
                    else if (!string.IsNullOrEmpty(foundryAlias) && foundryAlias != result.ModelId)
                    {
                        modelDisplay = $"{result.ModelId}<br/><small class=\"model-detail\">({foundryAlias})</small>";
                    }
                }
                
                html.AppendLine("                        <tr>");
                html.AppendLine($"                           <td><span class=\"provider-badge error\">{result.ProviderId}</span></td>");
                html.AppendLine($"                           <td class=\"model-cell\"><strong>{modelDisplay}</strong></td>");
                html.AppendLine($"                           <td class=\"error-message\">{System.Web.HttpUtility.HtmlEncode(errorMessage)}</td>");
                html.AppendLine($"                           <td><span class=\"duration\">{result.Duration.TotalMilliseconds:F0}ms</span></td>");
                html.AppendLine($"                           <td class=\"expandable\" title=\"{System.Web.HttpUtility.HtmlEncode(result.Prompt)}\">{System.Web.HttpUtility.HtmlEncode(truncatedPrompt)}</td>");
                html.AppendLine("                        </tr>");
            }

            html.AppendLine("                    </tbody>");
            html.AppendLine("                </table>");
        }

        private void GenerateMetricsSection(StringBuilder html, EvaluationSession session)
        {
            var resultsWithMetrics = session.Results.Where(r => r.Metrics != null).ToList();
            
            if (!resultsWithMetrics.Any()) return;

            html.AppendLine("        <section class=\"metrics\">");
            html.AppendLine("            <h2>Performance Metrics</h2>");
            html.AppendLine("            <div class=\"metrics-grid\">");

            foreach (var result in resultsWithMetrics)
            {
                var metrics = result.Metrics!;
                
                html.AppendLine("                <div class=\"metrics-card\">");
                html.AppendLine($"                   <h3>{result.ProviderId} - {result.ModelId}</h3>");
                html.AppendLine("                    <div class=\"metrics-details\">");
                html.AppendLine($"                       <div class=\"metric\">");
                html.AppendLine($"                           <span class=\"metric-label\">Avg CPU:</span>");
                html.AppendLine($"                           <span class=\"metric-value\">{metrics.AverageCpuUsage:F1}%</span>");
                html.AppendLine($"                       </div>");
                html.AppendLine($"                       <div class=\"metric\">");
                html.AppendLine($"                           <span class=\"metric-label\">Peak CPU:</span>");
                html.AppendLine($"                           <span class=\"metric-value\">{metrics.PeakCpuUsage:F1}%</span>");
                html.AppendLine($"                       </div>");
                html.AppendLine($"                       <div class=\"metric\">");
                html.AppendLine($"                           <span class=\"metric-label\">Avg Memory:</span>");
                html.AppendLine($"                           <span class=\"metric-value\">{metrics.AverageMemoryUsageMB:F0} MB</span>");
                html.AppendLine($"                       </div>");
                html.AppendLine($"                       <div class=\"metric\">");
                html.AppendLine($"                           <span class=\"metric-label\">Peak Memory:</span>");
                html.AppendLine($"                           <span class=\"metric-value\">{metrics.PeakMemoryUsageMB:F0} MB</span>");
                html.AppendLine($"                       </div>");
                
                if (metrics.AverageGpuUsage > 0)
                {
                    html.AppendLine($"                       <div class=\"metric\">");
                    html.AppendLine($"                           <span class=\"metric-label\">Avg GPU:</span>");
                    html.AppendLine($"                           <span class=\"metric-value\">{metrics.AverageGpuUsage:F1}%</span>");
                    html.AppendLine($"                       </div>");
                }
                
                if (metrics.AverageNpuUsage > 0)
                {
                    html.AppendLine($"                       <div class=\"metric\">");
                    html.AppendLine($"                           <span class=\"metric-label\">Avg NPU:</span>");
                    html.AppendLine($"                           <span class=\"metric-value\">{metrics.AverageNpuUsage:F1}%</span>");
                    html.AppendLine($"                       </div>");
                }
                
                // Token performance metrics
                if (metrics.TimeToFirstToken.HasValue)
                {
                    html.AppendLine($"                       <div class=\"metric\">");
                    html.AppendLine($"                           <span class=\"metric-label\">Time to First Token:</span>");
                    html.AppendLine($"                           <span class=\"metric-value\">{metrics.TimeToFirstToken.Value.TotalMilliseconds:F0}ms</span>");
                    html.AppendLine($"                       </div>");
                }
                
                html.AppendLine("                    </div>");
                html.AppendLine("                </div>");
            }

            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
        }

        private void GenerateChartsSection(StringBuilder html, EvaluationSession session)
        {
            html.AppendLine("        <section class=\"charts\">");
            html.AppendLine("            <h2>Performance Charts</h2>");
            html.AppendLine("            <div class=\"charts-grid\">");
            html.AppendLine("                <div class=\"chart-container\">");
            html.AppendLine("                    <canvas id=\"responseTimeChart\"></canvas>");
            html.AppendLine("                </div>");
            html.AppendLine("                <div class=\"chart-container\">");
            html.AppendLine("                    <canvas id=\"successRateChart\"></canvas>");
            html.AppendLine("                </div>");
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
        }

        private string GetCssStyles()
        {
            return @"
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }

                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                    line-height: 1.6;
                    color: #333;
                    background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
                    min-height: 100vh;
                }

                .container {
                    max-width: 1400px;
                    margin: 0 auto;
                    padding: 20px;
                }

                header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 40px;
                    border-radius: 15px;
                    margin-bottom: 30px;
                    text-align: center;
                    box-shadow: 0 10px 30px rgba(0,0,0,0.2);
                }

                header h1 {
                    font-size: 3rem;
                    margin-bottom: 15px;
                    font-weight: 700;
                    text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
                }

                .session-info {
                    opacity: 0.95;
                    margin: 8px 0;
                    font-size: 1.1rem;
                }

                section {
                    background: rgba(255, 255, 255, 0.95);
                    margin-bottom: 30px;
                    padding: 30px;
                    border-radius: 15px;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.1);
                    backdrop-filter: blur(10px);
                    border: 1px solid rgba(255,255,255,0.18);
                }

                h2 {
                    color: #2c3e50;
                    margin-bottom: 25px;
                    font-size: 2.2rem;
                    font-weight: 600;
                    border-bottom: 3px solid #3498db;
                    padding-bottom: 10px;
                }

                h3 {
                    color: #34495e;
                    margin-bottom: 20px;
                    font-size: 1.5rem;
                    font-weight: 600;
                }

                /* Quick Summary Cards */
                .quick-summary {
                    background: linear-gradient(135deg, #74b9ff 0%, #0984e3 100%);
                    color: white;
                }

                .quick-summary h2 {
                    color: white;
                    border-bottom-color: rgba(255,255,255,0.3);
                }

                .summary-cards {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                    gap: 25px;
                    margin-top: 20px;
                }

                .summary-card {
                    background: rgba(255, 255, 255, 0.15);
                    padding: 25px;
                    border-radius: 15px;
                    text-align: center;
                    backdrop-filter: blur(10px);
                    border: 1px solid rgba(255,255,255,0.2);
                    transition: transform 0.3s ease, box-shadow 0.3s ease;
                }

                .summary-card:hover {
                    transform: translateY(-5px);
                    box-shadow: 0 15px 35px rgba(0,0,0,0.2);
                }

                .summary-card.excellent {
                    border-left: 5px solid #00b894;
                }

                .summary-card.good {
                    border-left: 5px solid #00cec9;
                }

                .summary-card.warning {
                    border-left: 5px solid #fdcb6e;
                }

                .summary-card.poor {
                    border-left: 5px solid #e17055;
                }

                .summary-card.info {
                    border-left: 5px solid #74b9ff;
                }

                .card-icon {
                    font-size: 2.5rem;
                    margin-bottom: 10px;
                }

                .card-number {
                    font-size: 3rem;
                    font-weight: 700;
                    margin-bottom: 8px;
                    text-shadow: 1px 1px 2px rgba(0,0,0,0.1);
                }

                .card-label {
                    font-size: 1.2rem;
                    font-weight: 600;
                    margin-bottom: 5px;
                    opacity: 0.95;
                }

                .card-detail {
                    font-size: 0.95rem;
                    opacity: 0.8;
                }

                /* Results Section */
                .results-main {
                    background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
                }

                .results-status-tabs {
                    margin-bottom: 20px;
                    display: flex;
                    gap: 10px;
                    flex-wrap: wrap;
                }

                .tab-button {
                    background: #e9ecef;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 25px;
                    cursor: pointer;
                    font-weight: 600;
                    transition: all 0.3s ease;
                    font-size: 1rem;
                }

                .tab-button.active,
                .tab-button:hover {
                    background: #007bff;
                    color: white;
                    transform: translateY(-2px);
                    box-shadow: 0 5px 15px rgba(0,123,255,0.3);
                }

                .results-table {
                    overflow-x: auto;
                    background: white;
                    border-radius: 10px;
                    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                    margin-bottom: 20px;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                }

                th, td {
                    padding: 16px 12px;
                    text-align: left;
                    border-bottom: 1px solid #e9ecef;
                }

                th {
                    background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
                    font-weight: 700;
                    color: #495057;
                    font-size: 0.95rem;
                    text-transform: uppercase;
                    letter-spacing: 0.5px;
                    position: sticky;
                    top: 0;
                    z-index: 10;
                }

                tr:hover {
                    background-color: #f8f9fa;
                    transform: scale(1.01);
                    transition: all 0.2s ease;
                }

                .success {
                    color: #28a745;
                    font-weight: bold;
                    font-size: 1.2rem;
                }

                .error {
                    color: #dc3545;
                    font-weight: bold;
                    font-size: 1.2rem;
                }

                .provider-badge {
                    background: #007bff;
                    color: white;
                    padding: 4px 12px;
                    border-radius: 15px;
                    font-size: 0.85rem;
                    font-weight: 600;
                    text-transform: uppercase;
                }

                .provider-badge.error {
                    background: #dc3545;
                }

                .duration {
                    background: #e9ecef;
                    padding: 4px 8px;
                    border-radius: 8px;
                    font-weight: 600;
                    font-family: 'Courier New', monospace;
                }

                .performance {
                    padding: 6px 12px;
                    border-radius: 20px;
                    font-weight: 600;
                    font-size: 0.9rem;
                    text-align: center;
                }

                .performance.excellent {
                    background: #d4edda;
                    color: #155724;
                }

                .performance.good {
                    background: #d1ecf1;
                    color: #0c5460;
                }

                .performance.warning {
                    background: #fff3cd;
                    color: #856404;
                }

                .performance.poor {
                    background: #f8d7da;
                    color: #721c24;
                }

                .performance.neutral {
                    background: #e9ecef;
                    color: #495057;
                }

                .expandable {
                    max-width: 300px;
                    cursor: pointer;
                    transition: all 0.3s ease;
                }

                .expandable:hover {
                    background: #f0f0f0;
                    border-radius: 5px;
                }

                .model-cell {
                    min-width: 120px;
                }

                .model-detail {
                    color: #6c757d;
                    font-weight: 400;
                    font-size: 0.85rem;
                    font-style: italic;
                }

                .error-message {
                    color: #dc3545;
                    font-family: 'Courier New', monospace;
                    font-size: 0.9rem;
                    max-width: 400px;
                    word-break: break-word;
                }

                /* Legacy Summary Section (now detailed) */
                .summary-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                    gap: 20px;
                }

                .summary-item {
                    text-align: center;
                    padding: 20px;
                    background: #f8f9fa;
                    border-radius: 8px;
                    border-left: 4px solid #007bff;
                }

                .summary-number {
                    font-size: 2.5rem;
                    font-weight: bold;
                    color: #007bff;
                    margin-bottom: 5px;
                }

                .summary-label {
                    color: #6c757d;
                    font-weight: 500;
                }

                /* Metrics Section */
                .metrics-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
                    gap: 25px;
                }

                .metrics-card {
                    border: 1px solid #e9ecef;
                    border-radius: 12px;
                    padding: 25px;
                    background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
                    box-shadow: 0 4px 12px rgba(0,0,0,0.05);
                    transition: transform 0.3s ease, box-shadow 0.3s ease;
                }

                .metrics-card:hover {
                    transform: translateY(-3px);
                    box-shadow: 0 8px 25px rgba(0,0,0,0.1);
                }

                .metrics-card h3 {
                    color: #495057;
                    margin-bottom: 20px;
                    font-size: 1.3rem;
                    border-bottom: 2px solid #007bff;
                    padding-bottom: 8px;
                }

                .metrics-details {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 15px;
                }

                .metric {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    padding: 12px;
                    background: white;
                    border-radius: 8px;
                    border-left: 3px solid #007bff;
                    box-shadow: 0 2px 5px rgba(0,0,0,0.05);
                }

                .metric-label {
                    color: #6c757d;
                    font-weight: 600;
                    font-size: 0.9rem;
                }

                .metric-value {
                    font-weight: 700;
                    color: #495057;
                    font-size: 1rem;
                }

                /* Charts Section */
                .charts-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
                    gap: 30px;
                }

                .chart-container {
                    position: relative;
                    height: 350px;
                    background: white;
                    border-radius: 12px;
                    padding: 20px;
                    box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                }

                /* Device Metadata Styles */
                .device-metadata .device-info {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
                    gap: 25px;
                }

                .info-group {
                    border: 1px solid #e9ecef;
                    border-radius: 12px;
                    padding: 25px;
                    background: linear-gradient(135deg, #ffffff 0%, #f8f9fa 100%);
                    box-shadow: 0 4px 12px rgba(0,0,0,0.05);
                }

                .info-group h3 {
                    color: #495057;
                    margin-bottom: 20px;
                    font-size: 1.3rem;
                    border-bottom: 2px solid #007bff;
                    padding-bottom: 8px;
                }

                .info-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
                    gap: 15px;
                }

                .info-item {
                    display: flex;
                    flex-direction: column;
                    padding: 12px;
                    background: white;
                    border-radius: 8px;
                    border-left: 3px solid #007bff;
                    box-shadow: 0 2px 5px rgba(0,0,0,0.05);
                }

                .info-label {
                    font-size: 0.9rem;
                    color: #6c757d;
                    font-weight: 600;
                    margin-bottom: 4px;
                }

                .info-value {
                    font-size: 1rem;
                    color: #495057;
                    font-weight: 500;
                    word-break: break-word;
                }

                .gpu-list {
                    list-style: none;
                    padding: 0;
                }

                .gpu-list li {
                    padding: 12px 16px;
                    background: white;
                    border-radius: 8px;
                    margin-bottom: 10px;
                    border-left: 3px solid #28a745;
                    font-family: 'Courier New', monospace;
                    font-size: 0.9rem;
                    box-shadow: 0 2px 5px rgba(0,0,0,0.05);
                }

                /* Responsive Design */
                @media (max-width: 768px) {
                    .container {
                        padding: 15px;
                    }
                    
                    header h1 {
                        font-size: 2.2rem;
                    }
                    
                    .summary-cards {
                        grid-template-columns: 1fr;
                    }
                    
                    .metrics-details {
                        grid-template-columns: 1fr;
                    }
                    
                    .charts-grid {
                        grid-template-columns: 1fr;
                    }
                    
                    .device-metadata .device-info {
                        grid-template-columns: 1fr;
                    }
                    
                    .info-grid {
                        grid-template-columns: 1fr;
                    }
                    
                    .results-status-tabs {
                        flex-direction: column;
                    }
                    
                    .tab-button {
                        text-align: center;
                    }
                    
                    th, td {
                        padding: 12px 8px;
                        font-size: 0.9rem;
                    }
                }

                @media (max-width: 480px) {
                    section {
                        padding: 20px;
                    }
                    
                    .card-number {
                        font-size: 2.5rem;
                    }
                    
                    h2 {
                        font-size: 1.8rem;
                    }
                }
            ";
        }

        private string GetJavaScript(EvaluationSession session)
        {
            var successfulResults = session.Results.Where(r => r.IsSuccess).ToList();
            var providers = session.Results.GroupBy(r => r.ProviderId).ToList();
            
            var responseTimeData = string.Join(",", successfulResults.Select(r => r.Duration.TotalMilliseconds));
            var responseTimeLabels = string.Join(",", successfulResults.Select((r, i) => $"'{i + 1}'"));
            
            var successRateData = string.Join(",", providers.Select(g => 
                Math.Round((double)g.Count(r => r.IsSuccess) / g.Count() * 100, 1)));
            var successRateLabels = string.Join(",", providers.Select(g => $"'{g.Key}'"));

            return $@"
                // Tab functionality for results section
                function showTab(tabName) {{
                    // Hide all tabs
                    document.getElementById('all-results').style.display = 'none';
                    const successTab = document.getElementById('successful-results');
                    const failedTab = document.getElementById('failed-results');
                    if (successTab) successTab.style.display = 'none';
                    if (failedTab) failedTab.style.display = 'none';
                    
                    // Remove active class from all buttons
                    const buttons = document.querySelectorAll('.tab-button');
                    buttons.forEach(btn => btn.classList.remove('active'));
                    
                    // Show selected tab and mark button as active
                    if (tabName === 'all') {{
                        document.getElementById('all-results').style.display = 'block';
                        buttons[0].classList.add('active');
                    }} else if (tabName === 'successful') {{
                        if (successTab) successTab.style.display = 'block';
                        buttons[1].classList.add('active');
                    }} else if (tabName === 'failed') {{
                        if (failedTab) failedTab.style.display = 'block';
                        buttons[2].classList.add('active');
                    }}
                }}

                // Response Time Chart
                const responseTimeCtx = document.getElementById('responseTimeChart').getContext('2d');
                new Chart(responseTimeCtx, {{
                    type: 'line',
                    data: {{
                        labels: [{responseTimeLabels}],
                        datasets: [{{
                            label: 'Response Time (ms)',
                            data: [{responseTimeData}],
                            borderColor: '#667eea',
                            backgroundColor: 'rgba(102, 126, 234, 0.1)',
                            borderWidth: 3,
                            fill: true,
                            tension: 0.4,
                            pointBackgroundColor: '#667eea',
                            pointBorderColor: '#ffffff',
                            pointBorderWidth: 2,
                            pointRadius: 6
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {{
                            title: {{
                                display: true,
                                text: '⚡ Response Time Over Evaluations',
                                font: {{
                                    size: 18,
                                    weight: 'bold'
                                }},
                                color: '#2c3e50'
                            }},
                            legend: {{
                                labels: {{
                                    usePointStyle: true,
                                    font: {{
                                        size: 14
                                    }}
                                }}
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                title: {{
                                    display: true,
                                    text: 'Time (ms)',
                                    font: {{
                                        size: 14,
                                        weight: 'bold'
                                    }}
                                }},
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }},
                            x: {{
                                title: {{
                                    display: true,
                                    text: 'Evaluation #',
                                    font: {{
                                        size: 14,
                                        weight: 'bold'
                                    }}
                                }},
                                grid: {{
                                    color: 'rgba(0,0,0,0.1)'
                                }}
                            }}
                        }},
                        interaction: {{
                            intersect: false,
                            mode: 'index'
                        }}
                    }}
                }});

                // Success Rate Chart
                const successRateCtx = document.getElementById('successRateChart').getContext('2d');
                new Chart(successRateCtx, {{
                    type: 'doughnut',
                    data: {{
                        labels: [{successRateLabels}],
                        datasets: [{{
                            label: 'Success Rate (%)',
                            data: [{successRateData}],
                            backgroundColor: [
                                '#00b894',
                                '#74b9ff',
                                '#fdcb6e',
                                '#e17055',
                                '#a29bfe',
                                '#fd79a8',
                                '#00cec9'
                            ],
                            borderWidth: 3,
                            borderColor: '#ffffff',
                            hoverBorderWidth: 5
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {{
                            title: {{
                                display: true,
                                text: '📊 Success Rate by Provider',
                                font: {{
                                    size: 18,
                                    weight: 'bold'
                                }},
                                color: '#2c3e50'
                            }},
                            legend: {{
                                position: 'bottom',
                                labels: {{
                                    usePointStyle: true,
                                    padding: 20,
                                    font: {{
                                        size: 14
                                    }}
                                }}
                            }},
                            tooltip: {{
                                callbacks: {{
                                    label: function(context) {{
                                        return context.label + ': ' + context.parsed + '%';
                                    }}
                                }}
                            }}
                        }},
                        cutout: '50%'
                    }}
                }});

                // Add click handlers for expandable cells
                document.addEventListener('DOMContentLoaded', function() {{
                    const expandableCells = document.querySelectorAll('.expandable');
                    expandableCells.forEach(cell => {{
                        cell.addEventListener('click', function() {{
                            const fullText = this.getAttribute('title');
                            if (fullText && fullText !== this.textContent) {{
                                alert(fullText);
                            }}
                        }});
                    }});
                }});
            ";
        }
    }
}
