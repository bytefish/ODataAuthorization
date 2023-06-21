<#
.SYNOPSIS
    Example Script for restricting Navigation Properties using the 
    ODataAuthorization library.
.DESCRIPTION
    This script obtains a valid token and proceeds to perform requests to
    the API.
.NOTES
    File Name      : ODataQueries.ps1
    Author         : Philipp Wagner
    Prerequisite   : PowerShell
    Copyright 2023 - MIT License
#>

# Perform /Auth/login to obtain the JWT with Requested Scopes
$authRequestBody = @{
    Email = "admin@admin.com"
    Password = "123456"
    RequestedScopes = "Products.Read Products.ReadByKey"
}

$authRequestParameters = @{
    Method = "POST"
    Uri = "http://localhost:5124/Auth/login"
    Body = ($authRequestBody | ConvertTo-Json) 
    ContentType = "application/json"
}

# Invoke the Rest API
$authRequestResponse = Invoke-RestMethod @authRequestParameters

# Extract JWT from the JSON Response 
$authToken = $authRequestResponse.token

# The Auth Header needs to be sent for any additional OData request
$authHeader = @{
    Authorization = "Bearer $authToken"
}

# OData Query 1: All Products
$allProductsResponse = Invoke-RestMethod -Uri "http://localhost:5124/odata/Products" -Headers $authHeader 
$allProductsResponseValue = $allProductsResponse.value | ConvertTo-Json


Write-Host "[REQ]"
Write-Host "[REQ] OData Request #1"
Write-Host "[REQ]     URL:            http://localhost:5124/odata/Products"
Write-Host "[REQ]     Scopes:         Products.Read Products.ReadByKey"
Write-Host "[REQ]"

try {
    $allProductsResponse = Invoke-RestMethod -Uri "http://localhost:5124/odata/Products" -Headers $authHeader 
    $allProductsResponseValue = $allProductsResponse.value | ConvertTo-Json
    
    Write-Host "[RES]    HTTP Status:    $statusCode"  -ForegroundColor Green
    Write-Host "[RES]    Body:           $allProductsResponseValue"  -ForegroundColor Green
} catch {
    Write-Host "[RES] Request failed with StatusCode:" $_.Exception.Response.StatusCode.value__ -ForegroundColor Red
}

Write-Host "[REQ]"
Write-Host "[REQ] OData Request #2"
Write-Host "[REQ]     URL:            http://localhost:5124/odata/Products(`$expand=Address)"
Write-Host "[REQ]     Scopes:         Products.Read Products.ReadByKey"
Write-Host "[REQ]"

try {
    # OData Query 1: All Products
    $customersWithAddressResponse = Invoke-RestMethod -Uri "http://localhost:5124/odata/Products?`$expand=Address" -Headers $authHeader 
    $customersWithAddressResponseValue = $customersWithAddressResponse.value | ConvertTo-Json
    
    Write-Host "[RES]    HTTP Status:    $statusCode"  -ForegroundColor Green
    Write-Host "[RES]    Body:           $allProductsResponseValue"  -ForegroundColor Green
} catch {
    Write-Host "[RES] Request failed with StatusCode:" $_.Exception.Response.StatusCode.value__ -ForegroundColor Red
}

# Perform /Auth/login with additional Products.ReadAddress Scope
$authRequestBody = @{
    Email = "admin@admin.com"
    Password = "123456"
    RequestedScopes = "Products.Read Products.ReadByKey Products.ReadAddress"
}

$authRequestParameters = @{
    Method = "POST"
    Uri = "http://localhost:5124/Auth/login"
    Body = ($authRequestBody | ConvertTo-Json) 
    ContentType = "application/json"
}

# Invoke the Rest API
$authRequestResponse = Invoke-RestMethod @authRequestParameters

# Extract JWT from the JSON Response 
$authToken = $authRequestResponse.token

# The Auth Header needs to be sent for any additional OData request
$authHeader = @{
    Authorization = "Bearer $authToken"
}

Write-Host "[REQ]"
Write-Host "[REQ] OData Request #3"
Write-Host "[REQ]     URL:            http://localhost:5124/odata/Products(`$expand=Address)"
Write-Host "[REQ]     Scopes:         Products.Read Products.ReadByKey Products.ReadAddress" 
Write-Host "[REQ]" -ForegroundColor Green

try {
    # OData Query 1: All Products
    $customersWithAddressResponse = Invoke-RestMethod -Uri "http://localhost:5124/odata/Products?`$expand=Address" -Headers $authHeader 
    $customersWithAddressResponseValue = $customersWithAddressResponse.value | ConvertTo-Json
    
    Write-Host "[RES]    HTTP Status:    $statusCode"  -ForegroundColor Green
    Write-Host "[RES]    Body:           $allProductsResponseValue"  -ForegroundColor Green
} catch {
    Write-Host "[RES] Request failed with StatusCode:" $_.Exception.Response.StatusCode.value__ -ForegroundColor Red 
}