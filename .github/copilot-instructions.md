<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# ModelEvaluator Project Instructions

This is a cross-platform .NET C# console application for evaluating AI models and APIs with comprehensive metrics collection and HTML reporting.

## Project Architecture

- **Core**: Contains interfaces and base classes for the evaluation framework
- **Models**: Data models for evaluation results, metrics, and configuration
- **Providers**: AI model/API providers (OpenAI, Azure, WCR APIs, Local models)
- **Metrics**: System metrics collection (CPU, memory, GPU, NPU, latency)
- **Reporting**: HTML report generation and visualization
- **UI**: Command-line interface and user interaction

## Key Design Principles

1. **Extensibility**: Easy to add new model providers, metrics collectors, and report formats
2. **Cross-platform**: Works on Windows, macOS, and Linux
3. **Modular**: Separation of concerns with dependency injection
4. **Async/Await**: Non-blocking operations for API calls and metrics collection
5. **Configuration-driven**: JSON configuration for providers and settings

## Coding Standards

- Use async/await patterns for I/O operations
- Implement proper error handling and logging
- Follow SOLID principles
- Use dependency injection for loose coupling
- Implement comprehensive interfaces for testability
- Use CancellationToken for long-running operations
