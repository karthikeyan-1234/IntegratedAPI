# Make 10 requests to the Kong Gateway to test load balancing
for ($i=1; $i -le 10; $i++) {
    curl.exe -s http://localhost:8000/ | Out-Null
    Write-Host "Request $i sent"
}

# Check Kong metrics for total HTTP requests
curl.exe http://localhost:8001/metrics | Select-String -Pattern "http_requests_total"

# Define access points as PowerShell objects
$accessPoints = @(
    [PSCustomObject]@{ Service = "Kong Proxy"; URL = "http://localhost:8000"; Purpose = "Your API through Kong (USE THIS)" }
    [PSCustomObject]@{ Service = "Kong Manager"; URL = "http://localhost:8002"; Purpose = "Visual management UI" }
    [PSCustomObject]@{ Service = "Kong Admin API"; URL = "http://localhost:8001"; Purpose = "REST API for configuration" }
    [PSCustomObject]@{ Service = "Direct API"; URL = "http://localhost:7080"; Purpose = "Bypass Kong (for comparison)" }
    [PSCustomObject]@{ Service = "Prometheus Metrics"; URL = "http://localhost:8001/metrics"; Purpose = "Monitoring data" }
)

# Display access points table
$accessPoints | Format-Table -AutoSize

<#

Architecture Now Running:

                     ┌─────────────────────┐
                     │  Kong Manager UI    │
                     │  localhost:8002     │
                     └─────────────────────┘
                              │
                              ▼
┌──────────┐         ┌─────────────────┐         ┌─────────────┐
│ Clients  │────────▶│  Kong Gateway   │────────▶│ PostgreSQL  │
│          │         │  localhost:8000 │         │  (Kong DB)  │
└──────────┘         └─────────────────┘         └─────────────┘
                              │
                              │ Load Balance
                              ▼
                     ┌─────────────────┐
                     │ IntegratedAPI   │
                     │   Service       │
                     └─────────────────┘
                              │
                  ┌───────────┼───────────┐
                  ▼           ▼           ▼
               ┌─────┐    ┌─────┐    ┌─────┐
               │Pod 1│    │Pod 2│    │Pod 3│
               └─────┘    └─────┘    └─────┘
                  │           │           │
                  └───────────┼───────────┘
                              ▼
                     ┌─────────────────┐
                     │   SQL Server    │
                     │  127.0.0.1:1433 │
                     └─────────────────┘

#>
