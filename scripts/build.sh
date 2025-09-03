#!/bin/bash
# Build script for AI Model Evaluator (Linux/macOS)
echo "Building AI Model Evaluator..."
dotnet build ModelEvaluator.csproj
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fih
# Build script for AI Model Evaluator
echo "Building AI Model Evaluator..."
dotnet build ModelEvaluator.csproj
if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
else
    echo "❌ Build failed!"
    exit 1
fi
