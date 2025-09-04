using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace ModelEvaluator.Models
{
    /// <summary>
    /// Contains device and system metadata for evaluation reports
    /// </summary>
    public class DeviceMetadata
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string OSVersion { get; set; } = string.Empty;
        public string OSArchitecture { get; set; } = string.Empty;
        public string ProcessorName { get; set; } = string.Empty;
        public string ProcessorArchitecture { get; set; } = string.Empty;
        public int ProcessorCores { get; set; }
        public int LogicalProcessors { get; set; }
        public long TotalMemoryMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DotNetVersion { get; set; } = string.Empty;
        public List<string> GpuDevices { get; set; } = new();
        public Dictionary<string, string> AdditionalInfo { get; set; } = new();
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Collects device metadata from the current system
        /// </summary>
        public static DeviceMetadata Collect()
        {
            var metadata = new DeviceMetadata();

            try
            {
                // Basic system information
                metadata.OperatingSystem = Environment.OSVersion.Platform.ToString();
                metadata.OSVersion = Environment.OSVersion.VersionString;
                metadata.OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
                metadata.ProcessorArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
                metadata.ProcessorCores = Environment.ProcessorCount;
                metadata.MachineName = Environment.MachineName;
                metadata.UserName = Environment.UserName;
                metadata.DotNetVersion = Environment.Version.ToString();

                // Get more detailed OS information
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CollectWindowsSpecificInfo(metadata);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    CollectLinuxSpecificInfo(metadata);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    CollectMacOSSpecificInfo(metadata);
                }

                // Memory information (cross-platform fallback)
                if (metadata.TotalMemoryMB == 0)
                {
                    CollectCrossPlatformMemoryInfo(metadata);
                }

                // Additional system information
                metadata.AdditionalInfo["RuntimeIdentifier"] = RuntimeInformation.RuntimeIdentifier;
                metadata.AdditionalInfo["FrameworkDescription"] = RuntimeInformation.FrameworkDescription;
                metadata.AdditionalInfo["ProcessArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString();
                metadata.AdditionalInfo["OSDescription"] = RuntimeInformation.OSDescription;
            }
            catch (Exception ex)
            {
                metadata.AdditionalInfo["CollectionError"] = ex.Message;
            }

            return metadata;
        }

        private static void CollectWindowsSpecificInfo(DeviceMetadata metadata)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                // Try using WMI, but with platform checks
                try
                {
                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                    using (searcher)
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var totalPhysicalMemory = obj["TotalPhysicalMemory"];
                            if (totalPhysicalMemory != null)
                            {
                                metadata.TotalMemoryMB = Convert.ToInt64(totalPhysicalMemory) / (1024 * 1024);
                            }
                            break;
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    metadata.AdditionalInfo["WindowsWMI"] = "WMI not supported on this platform";
                }

                try
                {
                    var cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    using (cpuSearcher)
                    {
                        foreach (ManagementObject obj in cpuSearcher.Get())
                        {
                            var name = obj["Name"]?.ToString();
                            if (!string.IsNullOrEmpty(name))
                            {
                                metadata.ProcessorName = name.Trim();
                            }
                            
                            var logicalProcessors = obj["NumberOfLogicalProcessors"];
                            if (logicalProcessors != null)
                            {
                                metadata.LogicalProcessors = Convert.ToInt32(logicalProcessors);
                            }
                            break;
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    metadata.AdditionalInfo["ProcessorWMI"] = "Processor WMI not supported";
                    metadata.LogicalProcessors = Environment.ProcessorCount;
                }

                try
                {
                    var gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    using (gpuSearcher)
                    {
                        foreach (ManagementObject obj in gpuSearcher.Get())
                        {
                            var gpuName = obj["Name"]?.ToString();
                            if (!string.IsNullOrEmpty(gpuName))
                            {
                                metadata.GpuDevices.Add(gpuName);
                            }
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    metadata.AdditionalInfo["GPUWMI"] = "GPU WMI not supported";
                }

                try
                {
                    var memSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                    using (memSearcher)
                    {
                        foreach (ManagementObject memObj in memSearcher.Get())
                        {
                            var freeMemory = memObj["FreePhysicalMemory"];
                            if (freeMemory != null)
                            {
                                metadata.AvailableMemoryMB = Convert.ToInt64(freeMemory) / 1024; // Convert from KB to MB
                            }
                            
                            var osVersion = memObj["Version"]?.ToString();
                            if (!string.IsNullOrEmpty(osVersion))
                            {
                                metadata.OSVersion = $"Windows {osVersion}";
                            }
                            break;
                        }
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    metadata.AdditionalInfo["OSWMI"] = "OS WMI not supported";
                }
            }
            catch (Exception ex)
            {
                metadata.AdditionalInfo["WindowsInfoError"] = ex.Message;
                // Provide fallback values
                metadata.ProcessorName = "Unknown (WMI error)";
                metadata.LogicalProcessors = Environment.ProcessorCount;
            }
        }

        private static void CollectLinuxSpecificInfo(DeviceMetadata metadata)
        {
            try
            {
                // Read /proc/meminfo for memory information
                if (File.Exists("/proc/meminfo"))
                {
                    var meminfo = File.ReadAllLines("/proc/meminfo");
                    foreach (var line in meminfo)
                    {
                        if (line.StartsWith("MemTotal:"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && long.TryParse(parts[1], out long totalKB))
                            {
                                metadata.TotalMemoryMB = totalKB / 1024;
                            }
                        }
                        else if (line.StartsWith("MemAvailable:"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && long.TryParse(parts[1], out long availableKB))
                            {
                                metadata.AvailableMemoryMB = availableKB / 1024;
                            }
                        }
                    }
                }

                // Read /proc/cpuinfo for processor information
                if (File.Exists("/proc/cpuinfo"))
                {
                    var cpuinfo = File.ReadAllLines("/proc/cpuinfo");
                    var processorCount = 0;
                    foreach (var line in cpuinfo)
                    {
                        if (line.StartsWith("model name") && string.IsNullOrEmpty(metadata.ProcessorName))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                metadata.ProcessorName = parts[1].Trim();
                            }
                        }
                        else if (line.StartsWith("processor"))
                        {
                            processorCount++;
                        }
                    }
                    metadata.LogicalProcessors = processorCount;
                }

                // Try to get OS release information
                if (File.Exists("/etc/os-release"))
                {
                    var osRelease = File.ReadAllLines("/etc/os-release");
                    var prettyName = osRelease.FirstOrDefault(l => l.StartsWith("PRETTY_NAME="));
                    if (prettyName != null)
                    {
                        metadata.OSVersion = prettyName.Split('=', 2)[1].Trim('"');
                    }
                }
            }
            catch (Exception ex)
            {
                metadata.AdditionalInfo["LinuxInfoError"] = ex.Message;
            }
        }

        private static void CollectMacOSSpecificInfo(DeviceMetadata metadata)
        {
            try
            {
                // Use system_profiler for macOS information
                var systemProfiler = ExecuteCommand("system_profiler", "SPHardwareDataType");
                if (!string.IsNullOrEmpty(systemProfiler))
                {
                    var lines = systemProfiler.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.StartsWith("Processor Name:"))
                        {
                            metadata.ProcessorName = trimmedLine.Split(':', 2)[1].Trim();
                        }
                        else if (trimmedLine.StartsWith("Memory:"))
                        {
                            var memoryStr = trimmedLine.Split(':', 2)[1].Trim();
                            // Parse memory string like "16 GB"
                            var parts = memoryStr.Split(' ');
                            if (parts.Length >= 2 && double.TryParse(parts[0], out double memoryValue))
                            {
                                if (parts[1].ToUpper().Contains("GB"))
                                {
                                    metadata.TotalMemoryMB = (long)(memoryValue * 1024);
                                }
                            }
                        }
                    }
                }

                // Get macOS version
                var swVers = ExecuteCommand("sw_vers", "-productVersion");
                if (!string.IsNullOrEmpty(swVers))
                {
                    metadata.OSVersion = $"macOS {swVers.Trim()}";
                }
            }
            catch (Exception ex)
            {
                metadata.AdditionalInfo["MacOSInfoError"] = ex.Message;
            }
        }

        private static void CollectCrossPlatformMemoryInfo(DeviceMetadata metadata)
        {
            try
            {
                // Fallback: use GC information (less accurate but cross-platform)
                var totalMemory = GC.GetTotalMemory(false);
                metadata.AdditionalInfo["GCTotalMemory"] = $"{totalMemory / (1024 * 1024)} MB";
            }
            catch (Exception ex)
            {
                metadata.AdditionalInfo["CrossPlatformMemoryError"] = ex.Message;
            }
        }

        private static string ExecuteCommand(string command, string arguments)
        {
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return process.ExitCode == 0 ? output : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
