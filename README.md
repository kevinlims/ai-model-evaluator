# AI Model Evaluator

A cross-platform .NET console application for evaluating and comparing AI models from different providers with comprehensive metrics collection and HTML reporting.

## 🌟 Features

- **Multi-Provider Support**: OpenAI, Azure AI Foundry Local, and extensible for more providers
- **Comprehensive Metrics**: CPU, memory, GPU, NPU usage, and latency tracking with device metadata
- **Interactive CLI**: User-friendly command-line interface for model selection and evaluation
- **HTML Reports**: Beautiful, responsive HTML reports with charts and performance metrics
- **Multiple Evaluation Modes**: Single evaluations and batch evaluations with statistical analysis
- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Portable Deployment**: Self-contained Windows executable requiring no .NET installation
- **Extensible Architecture**: Easy to add new providers, metrics, and report formats

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 or later
- Optional: OpenAI API key for OpenAI provider
- Optional: Azure AI Foundry Local for local model evaluation

### Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd model_eval
   ```

2. **Build the project:**
   ```bash
   .\dev.ps1 build
   # or
   dotnet build
   ```

3. **Run the application:**
   ```bash
   .\dev.ps1 run
   # or
   dotnet run
   ```

## 🛠️ Development Commands

Use the main development script for all common tasks:

```powershell
# Build project
.\dev.ps1 build

# Run application
.\dev.ps1 run

# Build portable Windows executable
.\dev.ps1 portable

# Watch for changes and auto-restart
.\dev.ps1 watch

# Clean build artifacts
.\dev.ps1 clean

# Package for distribution
.\dev.ps1 package

# Show project status
.\dev.ps1 status
```

## 📦 Portable Windows Deployment

### Building Portable Executable

Create a self-contained Windows executable that runs without .NET installation:

```powershell
# Build portable executable
.\dev.ps1 portable

# Or use the script directly
.\scripts\build-portable.ps1
```

This creates:
- `dist/win-x64/ModelEvaluator.exe` (~76MB)
- `dist/ModelEvaluator-Portable-Windows-x64.zip` (distribution package)

### System Requirements for Portable Version

- Windows 10 version 1607+ or Windows Server 2016+
- x64 processor architecture
- **No .NET runtime installation required!**

## ⚙️ Configuration

### Environment Variables

#### OpenAI Provider
```powershell
# Windows PowerShell
$env:OPENAI_API_KEY="your-api-key-here"

# Windows Command Prompt
set OPENAI_API_KEY=your-api-key-here

# macOS/Linux
export OPENAI_API_KEY="your-api-key-here"
```

#### Azure OpenAI (Optional)
```powershell
$env:AZURE_OPENAI_API_KEY="your-azure-key-here"
$env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
```

### Configuration Files

Modify `appsettings.json` to customize:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ReportSettings": {
    "OutputDirectory": "reports",
    "AutoOpenReports": true
  }
}
```

## 🤖 Supported AI Providers

### Azure AI Foundry Local (azure-foundry-local)
- **No API key required** - runs local models
- **Models**: deepseek-r1-7b, deepseek-r1-14b, phi-3-mini-4k, phi-4, qwen2.5-7b, mistral-7b-v0.2, and more
- **Best for**: Local AI evaluation without cloud dependencies
- **Features**: Enhanced model metadata collection including foundry model IDs

### OpenAI (openai-demo)
- **Requires API key** - set `OPENAI_API_KEY` environment variable
- **Models**: gpt-4, gpt-4-turbo, gpt-3.5-turbo
- **Best for**: Cloud-based AI evaluation with latest OpenAI models

## 📊 Usage Examples

### Interactive Mode (Recommended)
```bash
.\dev.ps1 run
# Follow the interactive prompts to select providers, models, and enter prompts
```

### Command Line Usage
```bash
# Single evaluation
dotnet run -- --provider azure-foundry-local --model phi-3-mini-4k --prompt "Hello, world!"

# Multiple evaluations with HTML report
dotnet run -- --provider azure-foundry-local --model deepseek-r1-7b --prompt "Explain AI" --count 5 --report

# Using portable executable
.\dist\win-x64\ModelEvaluator.exe --provider azure-foundry-local --model phi-3-mini-4k --prompt "What is AI?"
```

### Example Workflows

#### Single Model Evaluation
1. Start the application
2. Select "2. Evaluate single model"
3. Choose Azure AI Foundry Local provider
4. Select a model (e.g., "phi-3-mini-4k")
5. Enter a prompt: "Explain quantum computing in simple terms"
6. View results with performance metrics
7. Generate HTML report when prompted

#### Multiple Evaluations with Statistics
1. Start the application
2. Select "3. Evaluate model multiple times (with averaged metrics)"
3. Choose provider and model
4. Set number of runs (e.g., 5)
5. Enter your prompt
6. Get statistical analysis with averaged metrics and confidence intervals

## 📈 Understanding Results

### Performance Metrics
- **Response Time**: Model inference duration
- **CPU Usage**: Average and peak CPU utilization
- **Memory Usage**: RAM consumption during execution
- **GPU Usage**: Graphics card utilization (when available)
- **NPU Usage**: Neural Processing Unit usage (when supported)
- **Device Metadata**: OS version, CPU architecture, memory specs

### HTML Report Features
- Interactive charts showing performance over time
- Success rate comparison between providers
- Detailed response analysis and metadata
- System resource utilization graphs
- Device and environment information
- Statistical analysis for multiple runs
- Responsive design for mobile viewing

## 🧪 Sample Prompts for Testing

### Code Generation
- "Write a Python function to sort a list of dictionaries by a specific key"
- "Create a REST API endpoint in C# for user authentication"
- "Implement a binary search algorithm in JavaScript"

### Reasoning & Analysis
- "Explain the differences between supervised and unsupervised learning"
- "How would you design a scalable chat application?"
- "Compare the pros and cons of microservices vs monolithic architecture"

### Creative Writing
- "Write a short story about a robot learning to paint"
- "Create a product description for a smart water bottle"
- "Compose a haiku about artificial intelligence"

## 🏗️ Project Architecture

```
src/
├── Core/           # Interfaces and base classes
├── Models/         # Data models for results and configuration
├── Providers/      # AI model/API providers
├── Metrics/        # System metrics collection
├── Reporting/      # HTML report generation
└── UI/            # Command-line interface

scripts/           # Build and development scripts
reports/          # Generated HTML evaluation reports
dist/            # Portable executable distributions
```

### Key Design Principles

1. **Extensibility**: Easy to add new model providers and metrics collectors
2. **Cross-platform**: Works on Windows, macOS, and Linux
3. **Modular**: Separation of concerns with dependency injection
4. **Async/Await**: Non-blocking operations for API calls and metrics collection
5. **Configuration-driven**: JSON configuration for providers and settings

## 🔧 Troubleshooting

### Common Issues

**Provider Not Available**
- Check API keys are set correctly
- Verify network connectivity for cloud providers
- Confirm Azure AI Foundry Local is installed for local models

**Performance Metrics Missing**
- Some metrics require administrator/root privileges
- GPU metrics need appropriate drivers installed
- NPU metrics require specific hardware and drivers

**Portable Executable Issues**
- Ensure Windows 10 version 1607+ for compatibility
- Run as Administrator if accessing system metrics
- Check antivirus software (may flag self-contained executables)

**High Memory Usage**
- Normal for self-contained executables (~70-80MB)
- Includes entire .NET runtime for portability

### Debug Information

1. **View logs** in the `logs/` directory for detailed error information
2. **Run with verbose logging** by editing `appsettings.json`
3. **Use help command**: `dotnet run -- --help`

## 📝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

### Adding New Providers

1. Implement the `IModelProvider` interface
2. Add provider registration in dependency injection
3. Update configuration and documentation
4. Add tests for the new provider

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🎯 Roadmap

- [ ] Support for additional AI providers (Anthropic, Cohere, etc.)
- [ ] Advanced prompt templating and testing scenarios
- [ ] Model fine-tuning evaluation capabilities
- [ ] RESTful API for programmatic access
- [ ] Web-based dashboard interface
- [ ] Integration with MLOps platforms

---

**Happy Evaluating! 🤖✨**

*Version: 1.0.0 | Built with .NET 8 | Cross-platform AI Model Evaluation*

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
