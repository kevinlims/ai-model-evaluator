# AI Model Evaluator

A cross-platform .NET console application for evaluating and comparing AI models from different providers with comprehensive metrics collection and HTML reporting.

## Features

- **Multi-Provider Support**: OpenAI, Azure, local models, and extensible for more providers
- **Comprehensive Metrics**: CPU, memory, GPU, NPU usage, and latency tracking
- **Interactive CLI**: User-friendly command-line interface for model selection and evaluation
- **HTML Reports**: Beautiful, responsive HTML reports with charts and performance metrics
- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Extensible Architecture**: Easy to add new providers, metrics, and report formats

## Quick Start

### Prerequisites

- .NET 8.0 or later
- Optional: OpenAI API key for OpenAI provider

### Installation

1. Clone or download this repository
2. Navigate to the project directory
3. Build the project:
   ```bash
   dotnet build
   ```

### Configuration

#### OpenAI Provider (Optional)
Set your OpenAI API key as an environment variable:

**Windows (PowerShell):**
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
```

**Windows (Command Prompt):**
```cmd
set OPENAI_API_KEY=your-api-key-here
```

**macOS/Linux:**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

### Running the Application

```bash
dotnet run
```

## Usage

The application provides an interactive menu with the following options:

1. **List Available Providers**: View all configured AI providers and their available models
2. **Evaluate Single Model**: Test a single model with a custom prompt
3. **Compare Multiple Models**: Compare performance across multiple models with the same prompt
4. **View Evaluation History**: Access previous evaluation sessions (future feature)
5. **Exit**: Close the application

### Example Workflow

1. Start the application
2. Select "List available providers" to see what's available
3. Choose "Evaluate single model" or "Compare multiple models"
4. Select your provider and model
5. Enter your prompt
6. Wait for evaluation and metrics collection
7. View results and optionally generate an HTML report

## Architecture

The application follows clean architecture principles with clear separation of concerns:

```
src/
├── Core/               # Interfaces and main services
│   ├── IModelProvider.cs
│   ├── IMetricsCollector.cs
│   ├── IReportGenerator.cs
│   ├── IEvaluationService.cs
│   └── EvaluationService.cs
├── Models/             # Data models
│   ├── EvaluationResult.cs
│   ├── EvaluationSession.cs
│   └── MetricsData.cs
├── Providers/          # AI model providers
│   └── OpenAIProvider.cs
├── Metrics/            # System metrics collection
│   └── SystemMetricsCollector.cs
├── Reporting/          # Report generation
│   └── HtmlReportGenerator.cs
└── UI/                 # User interface
    └── CommandLineInterface.cs
```

## Extending the Application

### Adding New Model Providers

1. Implement the `IModelProvider` interface
2. Register it in `Program.cs`
3. Add any required configuration

Example:
```csharp
public class MyCustomProvider : IModelProvider
{
    public string Id => "my-provider";
    public string Name => "My Custom Provider";
    public string Description => "Description of my provider";
    
    // Implement required methods...
}
```

### Adding New Metrics

1. Extend the `MetricsSnapshot` class with new properties
2. Update `SystemMetricsCollector` to collect the new metrics
3. Update report generators to display the new metrics

### Adding New Report Formats

1. Implement the `IReportGenerator` interface
2. Register it in `Program.cs`
3. The new format will be automatically available

## Performance Metrics

The application collects the following metrics during evaluation:

- **CPU Usage**: Average and peak CPU utilization
- **Memory Usage**: Average and peak memory consumption
- **GPU Usage**: Graphics card utilization (when available)
- **NPU Usage**: Neural Processing Unit utilization (when available)
- **Latency**: Response time from prompt to completion
- **Network I/O**: Network data transfer (future enhancement)
- **Disk I/O**: Disk read/write activity (future enhancement)

## HTML Reports

Generated reports include:

- **Executive Summary**: Key metrics and success rates
- **Detailed Results**: Complete evaluation results with responses
- **Performance Charts**: Interactive visualizations using Chart.js
- **Metrics Analysis**: Comprehensive performance breakdown
- **Responsive Design**: Works on desktop and mobile devices

## Supported Platforms

- **Windows**: Full feature support including Windows-specific performance counters
- **macOS**: Cross-platform metrics with macOS-specific optimizations
- **Linux**: Complete functionality with Linux system metrics
- **Docker**: Can be containerized for consistent deployment

## Configuration

The application uses .NET's built-in configuration system. You can configure providers through:

- Environment variables
- appsettings.json files
- Command-line arguments
- Azure Key Vault (for production deployments)

### Logging Configuration

The application supports configurable logging levels to control output verbosity:

#### Production (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Extensions.Hosting": "Warning",
      "ModelEvaluator": "Information"
    }
  },
  "Metrics": {
    "EnableDebugOutput": false
  }
}
```

#### Development (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ModelEvaluator": "Debug"
    }
  },
  "Metrics": {
    "EnableDebugOutput": true
  }
}
```

#### Environment Variable Override
You can enable debug metrics output temporarily:

**Windows:**
```cmd
set MODELEVALUATOR_DEBUG_METRICS=true
```

**macOS/Linux:**
```bash
export MODELEVALUATOR_DEBUG_METRICS=true
```

This will show detailed process tracking and memory breakdown information during model evaluation.

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Publishing

For a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained
dotnet publish -c Release -r osx-x64 --self-contained
dotnet publish -c Release -r linux-x64 --self-contained
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add your changes with appropriate tests
4. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Roadmap

- [ ] Azure OpenAI provider
- [ ] Anthropic Claude provider
- [ ] Google Gemini provider
- [ ] Hugging Face model support
- [ ] Database storage for evaluation history
- [ ] REST API for programmatic access
- [ ] Web UI for browser-based usage
- [ ] Advanced metrics (token usage, cost tracking)
- [ ] Model fine-tuning evaluation
- [ ] Batch processing capabilities
- [ ] Docker container support
- [ ] Kubernetes deployment manifests

## Support

For questions, issues, or feature requests, please create an issue in the GitHub repository.
