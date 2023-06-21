# JwtAuthenticationExample 

This example has been written by [@ika18](https://github.com/ika18) and shows how 
to use the ODataAuthorization library with Json Web Tokens (JWT). It comes with a 
Powershell Script to show some example OData requests.

The example shows how to protect the `Address` Navigation Property of the `Product` 
domain model. It requires a user to have the `Products.ReadAddress` scope for 
expanding or navigating to the `Product.Address`.

Here is the relevant Powershell snippet for creating the JWT with a set of scopes:

```powershell
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
```

We can see, that the first query for expanding the `Address` fails with HTTP Status 403 
(Permission Denied), because we haven't passed the `Products.ReadAddress` scope.

```
[REQ] OData Request #2
[REQ]     URL:            http://localhost:5124/odata/Products($expand=Address)
[REQ]     Scopes:         Products.Read Products.ReadByKey
[REQ]
[RES] Request failed with StatusCode: 403
```

The second request succeeds, because the `Products.ReadAddress` scope has been passed:

```
[REQ] OData Request #3
[REQ]     URL:            http://localhost:5124/odata/Products($expand=Address)
[REQ]     Scopes:         Products.Read Products.ReadByKey Products.ReadAddress
[REQ]
[RES]    HTTP Status:
[RES]    Body:           [
    {
        "Id":  1,
        "Name":  "Macbook M1",
        "Price":  3000,
        "AddressId":  1
    },
    {
        "Id":  2,
        "Name":  "Macbook M2",
        "Price":  3500,
        "AddressId":  1
    },
    {
        "Id":  3,
        "Name":  "iPhone 14",
        "Price":  1400,
        "AddressId":  1
    }
]
```

If you run it in the Powershell Console, you should get some nice colors:

<a href="https://raw.githubusercontent.com/bytefish/ODataAuthorization/main/samples/JwtAuthenticationExample/PowershellScripts/PowershellScriptScreenshot.jpg">
    <img src="https://raw.githubusercontent.com/bytefish/ODataAuthorization/main/samples/JwtAuthenticationExample/PowershellScripts/PowershellScriptScreenshot.jpg" alt="Powershell sample with Output" />
</a>
