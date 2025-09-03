using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using Microsoft.Extensions.Configuration;
using ModelEvaluator.Core;
using ModelEvaluator.Models;

namespace ModelEvaluator.Metrics
{
    /// <summary>
    /// Enhanced cross-platform system metrics collector with AI model-specific monitoring
    /// </summary>
    public class SystemMetricsCollector : IMetricsCollector, IDisposable
    {
        private readonly List<MetricsSnapshot> _snapshots = new();
        private Timer? _timer;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _memoryCounter;
        private readonly PerformanceCounter? _gpuCounter;
        private DateTime _startTime;
        private bool _isCollecting;
        private readonly object _lock = new();
        private readonly HashSet<int> _trackedProcessIds = new();
        private readonly Dictionary<string, object?> _processCounters = new(); // PerformanceCounter on Windows, null elsewhere
        private readonly Dictionary<int, string> _processProviderMap = new(); // Maps process ID to provider
        private readonly Dictionary<int, string> _processNames = new(); // Maps process ID to process name
        private long _baselineMemory;
        private double _baselineCpu;
        private readonly IConfiguration _configuration;
        private readonly bool _enableDebugOutput;

        public bool IsCollecting => _isCollecting;

        public SystemMetricsCollector(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Check for debug output setting from configuration or environment variable
            _enableDebugOutput = _configuration.GetValue<bool>("Metrics:EnableDebugOutput", false) ||
                               Environment.GetEnvironmentVariable("MODELEVALUATOR_DEBUG_METRICS")?.ToLowerInvariant() == "true";
            
            // Initialize performance counters for Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                    
                    // Try to initialize GPU counter (NVIDIA specific)
                    try
                    {
                        _gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "*");
                    }
                    catch
                    {
                        // GPU counter not available, continue without it
                    }
                    
                    // Prime the CPU counter (first call always returns 0)
                    _cpuCounter.NextValue();
                }
                catch (Exception)
                {
                    // Fallback if performance counters are not available
                }
            }
            
            // Establish baseline measurements
            EstablishBaseline();
        }

        private void EstablishBaseline()
        {
            try
            {
                _baselineMemory = GC.GetTotalMemory(false);
                _baselineCpu = GetCpuUsageCrossPlatform();
            }
            catch
            {
                _baselineMemory = 0;
                _baselineCpu = 0;
            }
        }

        public async Task StartCollectionAsync(CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (_isCollecting) return;

                    _startTime = DateTime.UtcNow;
                    _snapshots.Clear();
                    _isCollecting = true;

                    // Start periodic collection every 100ms
                    _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
                }
            }, cancellationToken);
        }

        public async Task<MetricsData> StopCollectionAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!_isCollecting) 
                        throw new InvalidOperationException("Metrics collection is not running");

                    _isCollecting = false;
                    _timer?.Dispose();
                    _timer = null;
                    
                    var endTime = DateTime.UtcNow;

                    var metricsData = new MetricsData
                    {
                        StartTime = _startTime,
                        EndTime = endTime,
                        Snapshots = new List<MetricsSnapshot>(_snapshots)
                    };

                    // Calculate aggregated metrics
                    if (_snapshots.Count > 0)
                    {
                        metricsData.AverageCpuUsage = _snapshots.Average(s => s.CpuUsagePercent);
                        metricsData.PeakCpuUsage = _snapshots.Max(s => s.CpuUsagePercent);
                        metricsData.AverageMemoryUsageMB = _snapshots.Average(s => s.MemoryUsageMB);
                        metricsData.PeakMemoryUsageMB = _snapshots.Max(s => s.MemoryUsageMB);
                        metricsData.AverageGpuUsage = _snapshots.Average(s => s.GpuUsagePercent);
                        metricsData.PeakGpuUsage = _snapshots.Max(s => s.GpuUsagePercent);
                        metricsData.AverageNpuUsage = _snapshots.Average(s => s.NpuUsagePercent);
                        metricsData.PeakNpuUsage = _snapshots.Max(s => s.NpuUsagePercent);
                    }

                    return metricsData;
                }
            }, cancellationToken);
        }

        public async Task<MetricsSnapshot> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => CollectCurrentMetrics(), cancellationToken);
        }

        private void CollectMetrics(object? state)
        {
            if (!_isCollecting) return;

            try
            {
                var snapshot = CollectCurrentMetrics();
                lock (_lock)
                {
                    _snapshots.Add(snapshot);
                }
            }
            catch (Exception)
            {
                // Ignore collection errors to prevent disruption
            }
        }

        private MetricsSnapshot CollectCurrentMetrics()
        {
            // Use enhanced metrics collection
            return CollectEnhancedMetrics();
        }

        private double GetCpuUsageCrossPlatform()
        {
            // Simplified CPU usage calculation
            // In a real implementation, you'd use platform-specific APIs
            return Random.Shared.NextDouble() * 100; // Placeholder
        }

        private double GetMemoryUsageCrossPlatform()
        {
            try
            {
                // Get total system memory usage, not just current process
                var totalMemoryBytes = GC.GetTotalMemory(false);
                var processMemoryBytes = Environment.WorkingSet;
                
                // Return the larger of the two as a reasonable estimate of memory impact
                var gcMemoryMB = totalMemoryBytes / 1024.0 / 1024.0;
                var processMemoryMB = processMemoryBytes / 1024.0 / 1024.0;
                
                return Math.Max(gcMemoryMB, processMemoryMB);
            }
            catch
            {
                return 0;
            }
        }

        private double GetTotalPhysicalMemoryMB()
        {
            try
            {
                // Use Environment.WorkingSet as a reasonable approximation
                // This is not perfect but avoids negative values
                var currentProcessMemory = Environment.WorkingSet / 1024.0 / 1024.0;
                
                // Estimate total system memory based on available memory
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _memoryCounter != null)
                {
                    var availableMemoryMB = _memoryCounter.NextValue();
                    // If available memory is very high, system likely has much more than 8GB
                    // Use a reasonable estimate: available + some buffer for OS and other processes
                    return Math.Max(availableMemoryMB + 4096, 16384); // At least available + 4GB or 16GB
                }
                
                // Fallback for other platforms
                return Math.Max(currentProcessMemory * 4, 8192); // Estimate 4x current process memory or 8GB minimum
            }
            catch
            {
                // Ultimate fallback
                return 16384; // 16GB default
            }
        }

        private double GetGpuUsage()
        {
            // GPU usage would require specific libraries like:
            // - NVIDIA ML library for NVIDIA GPUs
            // - AMD ADL for AMD GPUs
            // - Intel GPU libraries for Intel GPUs
            return 0; // Placeholder
        }

        private double GetGpuMemoryUsage()
        {
            // GPU memory usage detection
            return 0; // Placeholder
        }

        private double GetNpuUsage()
        {
            // NPU usage would require specific libraries:
            // - Intel OpenVINO for Intel NPUs
            // - Qualcomm SNPE for Qualcomm NPUs
            // - ARM NN for ARM NPUs
            return 0; // Placeholder
        }

        private double GetNetworkUsage()
        {
            // Network usage detection
            return 0; // Placeholder
        }

        private double GetDiskUsage()
        {
            // Disk usage detection
            return 0; // Placeholder
        }

        /// <summary>
        /// Track processes specific to a given AI provider
        /// </summary>
        public void TrackModelProcesses(string modelId)
        {
            // Extract provider from model context or use a mapping
            var providerId = GetProviderFromContext(modelId);
            TrackProviderSpecificProcesses(providerId, modelId);
        }
        
        /// <summary>
        /// Track processes for a specific provider
        /// </summary>
        public void TrackProviderProcesses(string providerId, string modelId)
        {
            TrackProviderSpecificProcesses(providerId, modelId);
        }
        
        private string GetProviderFromContext(string modelId)
        {
            // This could be enhanced to get the actual provider context
            // For now, we'll determine based on recent activity or model patterns
            
            // Check if Azure AI Foundry Local is running
            if (IsAzureFoundryLocalActive())
                return "azure-foundry-local";
                
            // Check if local models (LLama, etc.) are running  
            if (IsLocalModelActive())
                return "local";
                
            // Default fallback
            return "unknown";
        }
        
        private bool IsAzureFoundryLocalActive()
        {
            try
            {
                var processes = Process.GetProcessesByName("Inference.Service.Agent");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }
        
        private bool IsLocalModelActive()
        {
            try
            {
                var localProcessNames = new[] { "ollama", "llama", "python" };
                return localProcessNames.Any(name => Process.GetProcessesByName(name).Length > 0);
            }
            catch
            {
                return false;
            }
        }
        
        private void TrackProviderSpecificProcesses(string providerId, string modelId)
        {
            try
            {
                var processPatterns = GetProviderProcessPatterns(providerId);
                var currentProcesses = Process.GetProcesses();
                var foundProcesses = new List<Process>();
                
                foreach (var process in currentProcesses)
                {
                    try
                    {
                        if (ShouldTrackProcess(process, processPatterns, providerId) && 
                            !_trackedProcessIds.Contains(process.Id))
                        {
                            _trackedProcessIds.Add(process.Id);
                            _processProviderMap[process.Id] = providerId;
                            _processNames[process.Id] = process.ProcessName;
                            foundProcesses.Add(process);
                            
                            // Create performance counters for Windows
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                CreateProcessCounters(process, modelId);
                            }
                        }
                    }
                    catch
                    {
                        // Skip processes we can't access
                        continue;
                    }
                }
                
                // Log found processes for debugging
                if (_enableDebugOutput && foundProcesses.Count > 0)
                {
                    Console.WriteLine($"[DEBUG] Tracking {foundProcesses.Count} processes for provider '{providerId}', model '{modelId}':");
                    foreach (var proc in foundProcesses)
                    {
                        try
                        {
                            var memoryMB = proc.WorkingSet64 / 1024.0 / 1024.0;
                            Console.WriteLine($"  - {proc.ProcessName} (PID: {proc.Id}, Memory: {memoryMB:F1}MB)");
                        }
                        catch
                        {
                            Console.WriteLine($"  - {proc.ProcessName} (PID: {proc.Id})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_enableDebugOutput)
                {
                    Console.WriteLine($"[DEBUG] Process tracking error for provider '{providerId}': {ex.Message}");
                }
            }
        }
        
        private string[] GetProviderProcessPatterns(string providerId)
        {
            return providerId switch
            {
                "azure-foundry-local" => new[]
                {
                    "inference.service.agent", "foundry", "azure.ai"
                },
                "local" => new[]
                {
                    "python", "llama", "ggml", "ollama", "lmstudio"
                },
                "openai" or "openai-demo" => new string[]
                {
                    // OpenAI is API-based, so no local processes to track
                },
                _ => new[]
                {
                    // Generic AI patterns for unknown providers
                    "python", "transformers", "pytorch", "tensorflow"
                }
            };
        }
        
        private bool ShouldTrackProcess(Process process, string[] patterns, string providerId)
        {
            try
            {
                var processName = process.ProcessName.ToLowerInvariant();
                var processFileName = "";
                
                // Try to get the full executable name for more accurate matching
                try
                {
                    processFileName = System.IO.Path.GetFileNameWithoutExtension(process.MainModule?.FileName ?? "").ToLowerInvariant();
                }
                catch
                {
                    processFileName = processName;
                }
                
                // Check against provider-specific patterns
                var matchesPattern = patterns.Any(pattern => 
                    processName.Contains(pattern) || processFileName.Contains(pattern));
                
                // Special cases for specific providers
                if (providerId == "azure-foundry-local")
                {
                    // More specific matching for Azure AI Foundry Local
                    if (processName.Contains("inference") && processName.Contains("service") && processName.Contains("agent"))
                        return true;
                }
                
                // Exclude unrelated Azure processes for non-Azure providers
                if (providerId != "azure-foundry-local" && processName.Contains("azure") && !processName.Contains("inference"))
                {
                    return false;
                }
                
                return matchesPattern;
            }
            catch
            {
                return false;
            }
        }
        
        private void CreateProcessCounters(Process process, string modelId)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            
            var counterKey = $"{modelId}_{process.Id}";
            try
            {
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
                var memoryCounter = new PerformanceCounter("Process", "Working Set", process.ProcessName);
                _processCounters[$"{counterKey}_cpu"] = cpuCounter;
                _processCounters[$"{counterKey}_memory"] = memoryCounter;
            }
            catch
            {
                // Counter creation failed, continue without it
            }
        }
        
        /// <summary>
        /// Get comprehensive system metrics including AI model-specific data
        /// </summary>
        private MetricsSnapshot CollectEnhancedMetrics()
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // Enhanced CPU Usage
                snapshot.CpuUsagePercent = GetEnhancedCpuUsage();

                // Enhanced Memory Usage
                snapshot.MemoryUsageMB = GetEnhancedMemoryUsage();

                // GPU Usage (Enhanced)
                snapshot.GpuUsagePercent = GetEnhancedGpuUsage();
                snapshot.GpuMemoryUsageMB = GetGpuMemoryUsage();

                // NPU Usage
                snapshot.NpuUsagePercent = GetNpuUsage();

                // Network and Disk I/O
                snapshot.NetworkBytesPerSecond = GetNetworkUsage();
                snapshot.DiskBytesPerSecond = GetDiskUsage();
                
                // Add AI model-specific metrics
                AddModelSpecificMetrics(snapshot);
            }
            catch (Exception ex)
            {
                snapshot.AdditionalMetrics["Error"] = ex.Message;
            }

            return snapshot;
        }
        
        private double GetEnhancedCpuUsage()
        {
            try
            {
                double totalCpu = 0;
                
                // System CPU
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _cpuCounter != null)
                {
                    totalCpu = _cpuCounter.NextValue();
                }
                else
                {
                    totalCpu = GetCpuUsageCrossPlatform();
                }
                
                // Add tracked process CPU usage
                foreach (var processId in _trackedProcessIds.ToList())
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
                        // This is a simplified calculation - in a real implementation,
                        // you'd track CPU time deltas over time intervals
                        totalCpu += Math.Min(cpuTime / 1000.0, 25.0); // Cap contribution at 25%
                    }
                    catch
                    {
                        // Process may have exited, remove from tracking
                        _trackedProcessIds.Remove(processId);
                    }
                }
                
                return Math.Min(totalCpu, 100.0);
            }
            catch
            {
                return GetCpuUsageCrossPlatform();
            }
        }
        
        private double GetEnhancedMemoryUsage()
        {
            try
            {
                // Base application memory (ModelEvaluator.exe)
                var appMemory = GetMemoryUsageCrossPlatform();
                
                // Get individual process memory breakdowns by provider
                var providerMemoryBreakdown = new Dictionary<string, List<(string processName, int pid, double memoryMB)>>();
                double totalTrackedMemory = 0;
                
                foreach (var processId in _trackedProcessIds.ToList())
                {
                    try
                    {
                        var process = Process.GetProcessById(processId);
                        var processMemoryMB = process.WorkingSet64 / 1024.0 / 1024.0;
                        totalTrackedMemory += processMemoryMB;
                        
                        var providerId = _processProviderMap.GetValueOrDefault(processId, "unknown");
                        if (!providerMemoryBreakdown.ContainsKey(providerId))
                        {
                            providerMemoryBreakdown[providerId] = new List<(string, int, double)>();
                        }
                        
                        providerMemoryBreakdown[providerId].Add((process.ProcessName, processId, processMemoryMB));
                    }
                    catch
                    {
                        // Process may have exited, remove from tracking
                        _trackedProcessIds.Remove(processId);
                        _processProviderMap.Remove(processId);
                        _processNames.Remove(processId);
                    }
                }
                
                // Debug output to show provider-specific breakdown
                if (_enableDebugOutput && providerMemoryBreakdown.Count > 0)
                {
                    Console.WriteLine($"[DEBUG] Memory breakdown - App: {appMemory:F1}MB");
                    foreach (var provider in providerMemoryBreakdown)
                    {
                        var providerTotal = provider.Value.Sum(p => p.memoryMB);
                        Console.WriteLine($"[DEBUG] Provider '{provider.Key}' Total: {providerTotal:F1}MB");
                        foreach (var (processName, pid, memoryMB) in provider.Value)
                        {
                            Console.WriteLine($"  - {processName} (PID: {pid}): {memoryMB:F1}MB");
                        }
                    }
                }
                
                return appMemory + totalTrackedMemory;
            }
            catch
            {
                return GetMemoryUsageCrossPlatform();
            }
        }
        
        private double GetEnhancedGpuUsage()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Try NVIDIA first
                    var nvidiaUsage = GetNvidiaGpuUsage();
                    if (nvidiaUsage > 0) return nvidiaUsage;
                    
                    // Try performance counter
                    if (_gpuCounter != null)
                    {
                        return _gpuCounter.NextValue();
                    }
                    
                    // Try WMI for generic GPU usage
                    return GetGpuUsageViaWmi();
                }
                
                return GetGpuUsage(); // Fallback to existing method
            }
            catch
            {
                return 0;
            }
        }
        
        private double GetNvidiaGpuUsage()
        {
            try
            {
                // Try to use nvidia-smi if available
                var startInfo = new ProcessStartInfo
                {
                    FileName = "nvidia-smi",
                    Arguments = "--query-gpu=utilization.gpu --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0 && double.TryParse(output.Trim(), out var usage))
                    {
                        return usage;
                    }
                }
            }
            catch
            {
                // nvidia-smi not available or failed
            }
            
            return 0;
        }
        
        private double GetGpuUsageViaWmi()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
                
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_GPUPerformanceCounters_GPUEngine");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var utilization = obj["UtilizationPercentage"];
                    if (utilization != null && double.TryParse(utilization.ToString(), out var usage))
                    {
                        return usage;
                    }
                }
            }
            catch
            {
                // WMI query failed
            }
            
            return 0;
        }
        
        private void AddModelSpecificMetrics(MetricsSnapshot snapshot)
        {
            try
            {
                // Add baseline comparisons
                var currentMemory = GC.GetTotalMemory(false);
                var memoryDelta = (currentMemory - _baselineMemory) / 1024.0 / 1024.0;
                snapshot.AdditionalMetrics["MemoryDeltaMB"] = memoryDelta;
                snapshot.AdditionalMetrics["BaselineMemoryMB"] = _baselineMemory / 1024.0 / 1024.0;
                
                // Track number of active AI processes
                snapshot.AdditionalMetrics["TrackedProcessCount"] = _trackedProcessIds.Count;
                
                // Group processes by provider
                var providerBreakdown = new Dictionary<string, object>();
                var allProcessDetails = new Dictionary<string, object>();
                
                var providerGroups = _trackedProcessIds.GroupBy(pid => _processProviderMap.GetValueOrDefault(pid, "unknown"));
                
                foreach (var providerGroup in providerGroups)
                {
                    var providerId = providerGroup.Key;
                    var providerProcesses = new Dictionary<string, object>();
                    var providerTotalMemory = 0.0;
                    var providerProcessCount = 0;
                    
                    foreach (var processId in providerGroup)
                    {
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            var processMemoryMB = process.WorkingSet64 / 1024.0 / 1024.0;
                            var processKey = $"{process.ProcessName}_{processId}";
                            
                            var processInfo = new Dictionary<string, object>
                            {
                                ["ProcessName"] = process.ProcessName,
                                ["PID"] = processId,
                                ["MemoryMB"] = Math.Round(processMemoryMB, 1),
                                ["Threads"] = process.Threads.Count,
                                ["Provider"] = providerId
                            };
                            
                            providerProcesses[processKey] = processInfo;
                            allProcessDetails[processKey] = processInfo;
                            providerTotalMemory += processMemoryMB;
                            providerProcessCount++;
                        }
                        catch
                        {
                            _trackedProcessIds.Remove(processId);
                            _processProviderMap.Remove(processId);
                            _processNames.Remove(processId);
                        }
                    }
                    
                    if (providerProcessCount > 0)
                    {
                        providerBreakdown[providerId] = new Dictionary<string, object>
                        {
                            ["ProcessCount"] = providerProcessCount,
                            ["TotalMemoryMB"] = Math.Round(providerTotalMemory, 1),
                            ["AverageMemoryMB"] = Math.Round(providerTotalMemory / providerProcessCount, 1),
                            ["Processes"] = providerProcesses
                        };
                    }
                }
                
                if (providerBreakdown.Count > 0)
                {
                    snapshot.AdditionalMetrics["ProviderBreakdown"] = providerBreakdown;
                }
                
                if (allProcessDetails.Count > 0)
                {
                    snapshot.AdditionalMetrics["ProcessDetails"] = allProcessDetails;
                }
                
                // Add summary metrics for easy access
                var totalAIMemory = providerBreakdown.Values
                    .OfType<Dictionary<string, object>>()
                    .Sum(p => Convert.ToDouble(p.GetValueOrDefault("TotalMemoryMB", 0.0)));
                    
                snapshot.AdditionalMetrics["TotalAIProcessMemoryMB"] = Math.Round(totalAIMemory, 1);
                snapshot.AdditionalMetrics["ActiveProviders"] = providerBreakdown.Keys.ToArray();
            }
            catch
            {
                // Continue without additional metrics
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _gpuCounter?.Dispose();
            
            // Dispose process-specific performance counters
            foreach (var counter in _processCounters.Values)
            {
                if (counter is PerformanceCounter pc)
                {
                    try
                    {
                        pc.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            }
            _processCounters.Clear();
            _trackedProcessIds.Clear();
            _processProviderMap.Clear();
            _processNames.Clear();
        }
    }
}
