# Build script for AI Model Evaluator (PowerShell)
Write-Host "Building AI Model Evaluator..." -ForegroundColor Cyan
$result = & dotnet build ModelEvaluator.csproj
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
