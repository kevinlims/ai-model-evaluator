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
            html.AppendLine("            <h1>AI Model Evaluation Report</h1>");
            html.AppendLine($"           <p class=\"session-info\">Session ID: {session.Id}</p>");
            html.AppendLine($"           <p class=\"session-info\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            if (!string.IsNullOrEmpty(session.Description))
            {
                html.AppendLine($"           <p class=\"session-info\">Description: {session.Description}</p>");
            }
            html.AppendLine("        </header>");

            // Summary
            GenerateSummarySection(html, session);
            
            // Results
            GenerateResultsSection(html, session);
            
            // Metrics
            GenerateMetricsSection(html, session);
            
            // Charts
            GenerateChartsSection(html, session);

            html.AppendLine("    </div>");
            html.AppendLine("    <script>");
            html.AppendLine(GetJavaScript(session));
            html.AppendLine("    </script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private void GenerateSummarySection(StringBuilder html, EvaluationSession session)
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

        private void GenerateResultsSection(StringBuilder html, EvaluationSession session)
        {
            html.AppendLine("        <section class=\"results\">");
            html.AppendLine("            <h2>Evaluation Results</h2>");
            html.AppendLine("            <div class=\"results-table\">");
            html.AppendLine("                <table>");
            html.AppendLine("                    <thead>");
            html.AppendLine("                        <tr>");
            html.AppendLine("                            <th>Provider</th>");
            html.AppendLine("                            <th>Model</th>");
            html.AppendLine("                            <th>Prompt</th>");
            html.AppendLine("                            <th>Response</th>");
            html.AppendLine("                            <th>Duration</th>");
            html.AppendLine("                            <th>Status</th>");
            html.AppendLine("                        </tr>");
            html.AppendLine("                    </thead>");
            html.AppendLine("                    <tbody>");

            foreach (var result in session.Results)
            {
                var statusClass = result.IsSuccess ? "success" : "error";
                var truncatedPrompt = result.Prompt.Length > 100 ? result.Prompt.Substring(0, 100) + "..." : result.Prompt;
                var truncatedResponse = result.Response.Length > 200 ? result.Response.Substring(0, 200) + "..." : result.Response;
                
                html.AppendLine("                        <tr>");
                html.AppendLine($"                           <td>{result.ProviderId}</td>");
                html.AppendLine($"                           <td>{result.ModelId}</td>");
                html.AppendLine($"                           <td title=\"{System.Web.HttpUtility.HtmlEncode(result.Prompt)}\">{System.Web.HttpUtility.HtmlEncode(truncatedPrompt)}</td>");
                html.AppendLine($"                           <td title=\"{System.Web.HttpUtility.HtmlEncode(result.Response)}\">{System.Web.HttpUtility.HtmlEncode(truncatedResponse)}</td>");
                html.AppendLine($"                           <td>{result.Duration.TotalMilliseconds:F0}ms</td>");
                html.AppendLine($"                           <td class=\"{statusClass}\">{(result.IsSuccess ? "✓" : "✗")}</td>");
                html.AppendLine("                        </tr>");
            }

            html.AppendLine("                    </tbody>");
            html.AppendLine("                </table>");
            html.AppendLine("            </div>");
            html.AppendLine("        </section>");
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
                    background-color: #f5f5f5;
                }

                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 20px;
                }

                header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 30px;
                    border-radius: 10px;
                    margin-bottom: 30px;
                    text-align: center;
                }

                header h1 {
                    font-size: 2.5rem;
                    margin-bottom: 10px;
                }

                .session-info {
                    opacity: 0.9;
                    margin: 5px 0;
                }

                section {
                    background: white;
                    margin-bottom: 30px;
                    padding: 25px;
                    border-radius: 10px;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                }

                h2 {
                    color: #2c3e50;
                    margin-bottom: 20px;
                    font-size: 1.8rem;
                }

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

                .results-table {
                    overflow-x: auto;
                }

                table {
                    width: 100%;
                    border-collapse: collapse;
                    margin-top: 10px;
                }

                th, td {
                    padding: 12px;
                    text-align: left;
                    border-bottom: 1px solid #ddd;
                }

                th {
                    background-color: #f8f9fa;
                    font-weight: 600;
                    color: #495057;
                }

                tr:hover {
                    background-color: #f5f5f5;
                }

                .success {
                    color: #28a745;
                    font-weight: bold;
                }

                .error {
                    color: #dc3545;
                    font-weight: bold;
                }

                .metrics-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
                    gap: 20px;
                }

                .metrics-card {
                    border: 1px solid #e9ecef;
                    border-radius: 8px;
                    padding: 20px;
                    background: #f8f9fa;
                }

                .metrics-card h3 {
                    color: #495057;
                    margin-bottom: 15px;
                    font-size: 1.2rem;
                }

                .metrics-details {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 10px;
                }

                .metric {
                    display: flex;
                    justify-content: space-between;
                    padding: 8px 0;
                    border-bottom: 1px solid #dee2e6;
                }

                .metric-label {
                    color: #6c757d;
                }

                .metric-value {
                    font-weight: 600;
                    color: #495057;
                }

                .charts-grid {
                    display: grid;
                    grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
                    gap: 30px;
                }

                .chart-container {
                    position: relative;
                    height: 300px;
                }

                @media (max-width: 768px) {
                    .container {
                        padding: 10px;
                    }
                    
                    header h1 {
                        font-size: 2rem;
                    }
                    
                    .metrics-details {
                        grid-template-columns: 1fr;
                    }
                    
                    .charts-grid {
                        grid-template-columns: 1fr;
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
                // Response Time Chart
                const responseTimeCtx = document.getElementById('responseTimeChart').getContext('2d');
                new Chart(responseTimeCtx, {{
                    type: 'line',
                    data: {{
                        labels: [{responseTimeLabels}],
                        datasets: [{{
                            label: 'Response Time (ms)',
                            data: [{responseTimeData}],
                            borderColor: '#007bff',
                            backgroundColor: 'rgba(0, 123, 255, 0.1)',
                            borderWidth: 2,
                            fill: true
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {{
                            title: {{
                                display: true,
                                text: 'Response Time Over Evaluations'
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                title: {{
                                    display: true,
                                    text: 'Time (ms)'
                                }}
                            }},
                            x: {{
                                title: {{
                                    display: true,
                                    text: 'Evaluation #'
                                }}
                            }}
                        }}
                    }}
                }});

                // Success Rate Chart
                const successRateCtx = document.getElementById('successRateChart').getContext('2d');
                new Chart(successRateCtx, {{
                    type: 'bar',
                    data: {{
                        labels: [{successRateLabels}],
                        datasets: [{{
                            label: 'Success Rate (%)',
                            data: [{successRateData}],
                            backgroundColor: [
                                '#28a745',
                                '#007bff',
                                '#ffc107',
                                '#dc3545',
                                '#6f42c1'
                            ],
                            borderWidth: 1
                        }}]
                    }},
                    options: {{
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {{
                            title: {{
                                display: true,
                                text: 'Success Rate by Provider'
                            }}
                        }},
                        scales: {{
                            y: {{
                                beginAtZero: true,
                                max: 100,
                                title: {{
                                    display: true,
                                    text: 'Success Rate (%)'
                                }}
                            }}
                        }}
                    }}
                }});
            ";
        }
    }
}
