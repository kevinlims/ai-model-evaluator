using System;
using System.Threading;
using System.Threading.Tasks;
using ModelEvaluator.Models;

namespace ModelEvaluator.Core
{
    /// <summary>
    /// Interface for generating evaluation reports
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>
        /// Generate a report from evaluation results
        /// </summary>
        Task<string> GenerateReportAsync(EvaluationSession session, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Report format (e.g., "HTML", "JSON", "PDF")
        /// </summary>
        string Format { get; }
        
        /// <summary>
        /// File extension for the report
        /// </summary>
        string FileExtension { get; }
    }
}
