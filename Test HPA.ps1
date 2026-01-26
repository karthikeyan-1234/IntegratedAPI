# ============================================
# HPA Load Testing Script for IntegratedAPI
# ============================================

Write-Host "`n=== Starting HPA Auto-Scaling Load Test ===" -ForegroundColor Green
Write-Host "This will generate sustained load to trigger HPA scaling" -ForegroundColor Yellow
Write-Host "Watch your HPA monitor window to see scaling in action!`n" -ForegroundColor Yellow

# Configuration - UPDATE THIS TO YOUR ACTUAL API ENDPOINT
$apiUrl = "http://kong-proxy.local/WeatherForecast"  # Use your actual working endpoint
$duration = 300  # 5 minutes (300 seconds) - adjust as needed
$requestsPerSecond = 800  # Increase this to generate more CPU load
$delayBetweenRequests = [math]::Floor(1000 / $requestsPerSecond)  # milliseconds

$endTime = (Get-Date).AddSeconds($duration)
$requestCount = 0
$successCount = 0
$errorCount = 0

Write-Host "Configuration:" -ForegroundColor Cyan
Write-Host "  Target URL: $apiUrl" -ForegroundColor White
Write-Host "  Duration: $duration seconds" -ForegroundColor White
Write-Host "  Requests/sec: $requestsPerSecond" -ForegroundColor White
Write-Host "  Delay: $delayBetweenRequests ms`n" -ForegroundColor White

Write-Host "Starting load generation..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop early`n" -ForegroundColor Yellow

# Main load generation loop
while ((Get-Date) -lt $endTime) {
    $requestCount++
    
    try {
        # Send request and capture HTTP status code
        $response = curl.exe -s -o nul -w "%{http_code}" $apiUrl 2>$null
        
        # Check if response is a success code (2xx or 3xx)
        if ($response -match "^[23]\d\d$") {
            $successCount++
            $status = "[OK]"
            $color = "Green"
        } else {
            $errorCount++
            $status = "[FAIL-$response]"
            $color = "Red"
        }
    } catch {
        $errorCount++
        $status = "[ERROR]"
        $color = "Red"
    }
    
    # Progress update every 10 requests
    if ($requestCount % 10 -eq 0) {
        $remaining = ($endTime - (Get-Date)).TotalSeconds
        $remainingRounded = [math]::Round($remaining)
        Write-Host "$(Get-Date -Format 'HH:mm:ss') | Requests: $requestCount | Success: $successCount | Errors: $errorCount | Remaining: ${remainingRounded}s" -ForegroundColor Cyan
    } else {
        Write-Host "$status " -NoNewline -ForegroundColor $color
    }
    
    # Delay between requests
    Start-Sleep -Milliseconds $delayBetweenRequests
}

# Final summary
Write-Host "`n"
Write-Host "=== Load Test Completed ===" -ForegroundColor Green
Write-Host "Total Requests Sent: $requestCount" -ForegroundColor White
Write-Host "Successful Requests: $successCount" -ForegroundColor Green
Write-Host "Failed Requests: $errorCount" -ForegroundColor Red
if ($requestCount -gt 0) {
    $successRate = [math]::Round(($successCount / $requestCount) * 100, 2)
    Write-Host "Success Rate: $successRate%" -ForegroundColor Cyan
}

# Check current HPA status
Write-Host "`n=== Current HPA Status ===" -ForegroundColor Yellow
kubectl get hpa integratedapi-hpa

# Show current pod count
Write-Host "`n=== Current Pod Count ===" -ForegroundColor Yellow
kubectl get pods -l app=integratedapi-deployment

Write-Host "`nNote: Pods will scale DOWN after 5 minutes (300s stabilization window)" -ForegroundColor Yellow
Write-Host "Keep monitoring your HPA window to see the scale-down behavior!" -ForegroundColor Yellow