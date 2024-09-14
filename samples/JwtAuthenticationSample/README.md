# JwtAuthenticationExample 

This example has been written by [@ika18](https://github.com/ika18) and shows how 
to use the ODataAuthorization library with Json Web Tokens (JWT). It comes with a 
Powershell Script to show some example OData requests.

The example shows how to protect the `Address` Navigation Property of the `Product` 
domain model. It requires a user to have the `Products.ReadAddress` scope for 
expanding or navigating to the `Product.Address`.
