# AI Model Evaluator - Example Usage

This document provides examples of how to use the AI Model Evaluator.

## Setup

### 1. Set OpenAI API Key (Optional)
```bash
# Windows PowerShell
$env:OPENAI_API_KEY="your-api-key-here"

# Windows Command Prompt  
set OPENAI_API_KEY=your-api-key-here

# macOS/Linux
export OPENAI_API_KEY="your-api-key-here"
```

### 2. Run the Application
```bash
dotnet run
```

## Example Workflows

### Single Model Evaluation
1. Start the application
2. Select "2. Evaluate single model"
3. Choose a provider (e.g., "Local Models")
4. Select a model (e.g., "llama-2-7b")
5. Enter a prompt: "Explain quantum computing in simple terms"
6. View results and metrics
7. Generate HTML report when prompted

### Compare Multiple Models
1. Start the application
2. Select "3. Compare multiple models"
3. Enter a prompt: "Write a Python function to calculate fibonacci numbers"
4. Add multiple models:
   - Local Models: llama-2-7b
   - Local Models: mistral-7b
   - OpenAI: gpt-3.5-turbo (if API key configured)
5. View comparison results
6. Generate comprehensive report

## Sample Prompts for Testing

### Code Generation
- "Write a Python function to sort a list of dictionaries by a specific key"
- "Create a REST API endpoint in C# for user authentication"
- "Implement a binary search algorithm in JavaScript"

### Reasoning
- "Explain the differences between supervised and unsupervised learning"
- "How would you design a scalable chat application?"
- "What are the pros and cons of microservices architecture?"

### Creative Writing
- "Write a short story about a robot learning to paint"
- "Create a product description for a smart water bottle"
- "Compose a haiku about artificial intelligence"

## Understanding the Results

### Performance Metrics
- **Response Time**: How long the model took to generate a response
- **CPU Usage**: Average and peak CPU utilization during inference
- **Memory Usage**: RAM consumption during model execution
- **GPU Usage**: Graphics card utilization (when available)
- **NPU Usage**: Neural Processing Unit usage (when available)

### HTML Report Features
- Interactive charts showing performance over time
- Success rate comparison between providers
- Detailed response analysis
- System resource utilization graphs
- Responsive design for mobile viewing

## Tips for Best Results

1. **Consistent Prompts**: Use the same prompt when comparing models for fair evaluation
2. **Multiple Runs**: Run evaluations multiple times to account for variability
3. **Resource Monitoring**: Close other applications for more accurate metrics
4. **Model Selection**: Test both small and large models to understand trade-offs
5. **Prompt Engineering**: Try different prompt styles to test model capabilities

## Troubleshooting

### Common Issues

**Provider Not Available**
- Check API keys are set correctly
- Verify network connectivity
- Confirm model files exist (for local models)

**Performance Metrics Missing**
- Some metrics require administrator/root privileges
- GPU metrics need appropriate drivers installed
- NPU metrics require specific hardware and drivers

**Report Generation Fails**
- Check disk space availability
- Verify write permissions in output directory
- Ensure all evaluation data is complete
