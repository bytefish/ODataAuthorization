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


function Send-ODataRequest {
    param (
        [Parameter(Mandatory)]
        [string]$AuthUrl,

        [Parameter(Mandatory)]
        [string]$AuthEmail,

        [Parameter(Mandatory)]
        [string]$AuthPassword,

        [Parameter(Mandatory)]
        [string]$Description,

        [Parameter(Mandatory)]
        [string]$Endpoint,
        
        [Parameter(Mandatory)]
        [string]$RequestedScopes
    )
        
    # Perform /Auth/login to obtain the JWT with Requested Scopes
    $authRequestBody = @{
        Email = $AuthEmail
        Password = $AuthPassword
        RequestedScopes = $RequestedScopes
    }

    $authRequestParameters = @{
        Method = "POST"
        Uri = $AuthUrl
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
    Write-Host "[REQ] OData Request"
    Write-Host "[REQ]    URL:            $Endpoint"
    Write-Host "[REQ]    Scopes:         $RequestedScopes"
    Write-Host "[REQ]"

    $odataRequestParameters = @{
        Method = "GET"
        Uri = $Endpoint
        Headers = $authHeader
        StatusCodeVariable = 'statusCode'
    }

    try {
        
        $oDataResponse = Invoke-RestMethod @odataRequestParameters
        $oDataResponseValue = $oDataResponse.value | ConvertTo-Json
        
        Write-Host "[RES]    HTTP Status:    $statusCode"  -ForegroundColor Green
        Write-Host "[RES]    Body:           $oDataResponseValue"  -ForegroundColor Green
    } catch {
        Write-Host "[ERR] Request failed with StatusCode:" $_.Exception.Response.StatusCode.value__ -ForegroundColor Red
    }
}

$authUrl = "http://localhost:5124/Auth/login"
$authEmail = "admin@admin.com"
$authPassword = "123456"

$requests = 
    @{
        AuthUrl = $authUrl
        AuthEmail = $authEmail 
        AuthPassword = $authPassword 
        Description = "Get all Products without $expand"
        Endpoint = "http://localhost:5124/odata/Products" 
        RequestedScopes = "Products.Read Products.ReadByKey"
    },    
    @{
        AuthUrl = $authUrl
        AuthEmail = $authEmail 
        AuthPassword = $authPassword 
        Description = "Get all Products with an $expand on the 'Address' navigation property, but missing 'Products.ReadAddress' scope"
        Endpoint = "http://localhost:5124/odata/Products?`$expand=Address" 
        RequestedScopes = "Products.Read Products.ReadByKey"
    },
    @{
        AuthUrl = $authUrl
        AuthEmail = $authEmail 
        AuthPassword = $authPassword 
        Description = "Get all Products with an $expand on the 'Address' navigation, 'Products.ReadAddress' scope is included"
        Endpoint = "http://localhost:5124/odata/Products?`$expand=Address" 
        RequestedScopes = "Products.Read Products.ReadByKey Products.ReadAddress"
    }

foreach ( $request in $requests )
{
    Send-ODataRequest @request
}