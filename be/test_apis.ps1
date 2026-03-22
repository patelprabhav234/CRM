$ErrorActionPreference = "Stop"
$baseUri = "http://localhost:5254"

Write-Output "Starting tests..."

try {
    $body = @{
        CompanyName = "Test$(Get-Random)"
        Subdomain = "test-comp-$(Get-Random)"
        Email = "test$(Get-Random)@test.com"
        Password = "password123"
        Name = "Test User"
    }
    $json = $body | ConvertTo-Json
    $res = Invoke-RestMethod -Uri "$baseUri/api/Auth/register-tenant" -Method Post -Body $json -ContentType "application/json"
    $token = $res.token
    Write-Output "Registered successfully"
} catch {
    Write-Output "Registration failed: $($_.Exception.Message)"
    exit 1
}

$headers = @{
    Authorization = "Bearer $token"
}

$endpoints = @(
    "/api/AmcContracts",
    "/api/AmcVisits",
    "/api/Auth/me",
    "/api/Customers",
    "/api/Dashboard/summary",
    "/api/Installations",
    "/api/Leads",
    "/api/OpsTasks",
    "/api/Products",
    "/api/Quotations",
    "/api/ServiceRequests"
)

foreach ($ep in $endpoints) {
    try {
        $r = Invoke-RestMethod -Uri "$baseUri$ep" -Method Get -Headers $headers
        Write-Output "GET $ep - Success"
    } catch {
        Write-Output "GET $ep - Failed: $($_.Exception.Message)"
    }
}
Write-Output "Done"
