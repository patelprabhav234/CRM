function Test-Endpoint($Name, $Url, $Token) {
    Write-Host "Testing $Name... " -NoNewline
    try {
        $headers = @{ "Authorization" = "Bearer $Token" }
        $resp = Invoke-RestMethod -Uri "http://localhost:5247/api/$Url" -Method Get -Headers $headers
        Write-Host "SUCCESS (Items: $($resp.Count))" -ForegroundColor Green
    } catch {
        Write-Host "FAILED ($($_.Exception.Message))" -ForegroundColor Red
    }
}

$registerBody = @{
    CompanyName = "Azure Test"
    Subdomain = "azure-test-$(Get-Random)"
    Email = "test@example.com"
    Password = "password123"
    Name = "Test User"
} | ConvertTo-Json

Write-Host "Registering tenant on Azure DB..."
try {
    $auth = Invoke-RestMethod -Uri "http://localhost:5247/api/auth/register-tenant" -Method Post -Body $registerBody -ContentType "application/json"
    $token = $auth.token
    Write-Host "Tenant registered! Getting Token." -ForegroundColor Green
    
    Test-Endpoint "Dashboard" "dashboard/summary" $token
    Test-Endpoint "Leads" "leads" $token
    Test-Endpoint "Customers" "customers" $token
    Test-Endpoint "Products" "products" $token
    Test-Endpoint "Quotations" "quotations" $token
    Test-Endpoint "Installations" "installations" $token
    Test-Endpoint "AMC Contracts" "amccontracts" $token
    Test-Endpoint "Service Requests" "servicerequests" $token
    Test-Endpoint "Tasks" "opstasks" $token
} catch {
    Write-Host "REGISTRATION FAILED: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Error details: $($reader.ReadToEnd())"
    }
}
