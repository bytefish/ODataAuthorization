# ODataAuthorization

This library is a fork of the `Microsoft.AspNetCore.OData.Authorization` library, which is available at:

* [https://github.com/OData/WebApiAuthorization](https://github.com/OData/WebApiAuthorization)

It uses the permissions defined in the [capability annotations] of the OData model to apply authorization policies to an OData 
service based on `Microsoft.AspNetCore.OData`. This is done by adding an OData Policy, when configuring a services 
Authorization.

The library has been renamed to `ODataAuthorization` to avoid being mistaken as an official Microsoft package. As of 
version `2.0` the APIs differ greatly, so please read carefully when updating this dependency.

[capability annotations]: https://github.com/oasis-tcs/odata-vocabularies/blob/master/vocabularies/Org.OData.Capabilities.V1.md

## Usage

In your `Program.cs` file you'll need to add the Policy and Require it for your Endpoints:

```csharp
using Microsoft.AspNetCore.OData.Authorization

// ...

builder.Services.AddAuthorization(options =>
{
    options.AddODataAuthorizationPolicy();
});

// ...

var app = builder.Build();

// ...


app
    .MapControllers()
    .RequireODataAuthorization();
```

The Policy only applies to OData-enabled endpoints. Non-OData endpoints are ignored.

## Sample applications

- [ODataAuthorizationSample](./samples/ODataAuthorizationSample): Simple API with permission restrictions and OData authorization middleware set up with a custom authentication handler
- [CookieAuthenticationSample](./samples/CookieAuthenticationSample): Basic API with permissions restrictions and a cookie-based authentication handler

### How to specify permission scopes?

By default, the library will try extract permissions from the authenticated user's claims. Specifically, it will look for claims 
with the key `Scope`. If your app is storing user scopes differently (e.g. using a different key), you can pass your own 
function to the policy: 

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddODataAuthorizationPolicy("MyPolicy", (user) => user
                            .FindAll("PermissionScope")
                            .Select(claim => claim.Value));
});
```

For a complete working example, check [the sample application](samples/ODataAuthorizationSample).

## How permissions are applied

On each request, the library extracts from the model the permissions restrictions that should apply to the route being accessed and creates an authorization policy based on those permissions. Deeper down the request pipeline, the AspNetCore filter-based authorization system will call the OData authorization handler to verify whether the current user's permissions match the ones required by the policy.

**Note**: If there are not permission restrictions defined for an some target (entity set/singleton/operation) in the model, then endpoints to that target will be authorized by default regardless of the user's permissions.

### Example permission scopes

For the following examples, let's assume that we are working with an OData model that has the following scopes defined:


Permission scope name       | Where it's defined
----------------------------|--------------------
`Customers.Read`            | `ReadRestrictions` of `Customers` entity set
`Customers.ReadByKey`       | `ReadByKeyRestrictions` of `Customers`
`Customers.Insert`          | `InsertRestrictions` of `Customers`
`Customers.Delete`          | `DeleteRestrictions` of `Customers`
`Customers.Update`          | `UpdateRestrictions` of `Customers`
`CustomerOrders.Read`       | `ReadRestrictions` of `NavigationRestrictions` of `Customers` on `Orders` property
`CustomerOrders.ReadByKey`  | `ReadByKeyRestriction` of `NavigationRestrictions` of `Customers` on `Orders` property
`CustomerOrders.Insert`     | `InsertRestrictions` of `NavigationRestrictions` of `Customers` on `Orders` property
`CustomerOrders.Update`     | `UpdateRestrictions` of `NavigatonRestrictions` of `Customers` on `Orders` property
`CustomerOrders.Delete`     | `UpdateRestrictions` of `NavigationRestrictions` of `Customers` on `Orders` property
`Orders.Read`               | `ReadRestrictions` of `Orders` entity set
`Orders.ReadByKey`          | `ReadByKeyRestrictions` of `Orders`
`Orders.Update`             | `UpdateRestrictions` of `Orders`
`Orders.Delete`             | `DeleteRestrictions` of `Orders`
`Orders.Insert`             | `InsertRestrictions` of `Orders`
`Order.CalculateTax`        | `OperationRestrictions` of `CalculateTax` bound function
`UpdateTaxRate`             | `OperationRestrictions` of `UpdateTaxRate` unbound action
`TopProduct.Read`           | `ReadRestrictions` of `TopProduct` singleton

### CRUD operations on entity sets and singleton

For CRUD operations on entity sets and singleton, the permissions of the corresponding insert/update/delete/read restrictions are applied.

Endpoint                     | Required permission scopes
-----------------------------|----------------------
`GET Customers`              | `Customers.Read`
`GET Customers(1)`           | `Customers.Read` OR Customers.ReadByKey`
`DELETE Customers/1`         | `Customers.Delete`
`POST Customers`             | `Customers.Insert`
`PUT Customers`              | `Customers.Update`
`PATCH Customers`            | `Customers.Update`

Note, in the case of `Customers(1)`, permissions will be extracted from two places if available. Permissions will be extracted from both `ReadRestrictions`
as well as the `ReadByKeyRestrictions` property of the `ReadRestrictions`. If the user has any of the permissions defined in either the `ReadRestrictions` or
`ReadByKeyRestrictions`, then access will be granted.

For example, if the model defines permission scopes `Customers.Read` in the `ReadRestrictions`, and `Customers.ReadByKey` in the `ReadByKeyRestrictions`, then access to the `GET Customers(1)` endpoint will be granted to uers with either the `Customers.Read` or `Customers.ReadByKey` permissions.

### Function and Action calls

The `OperationRestricitons` of the function or action are applied. For function and action imports, the `OperationRestrictions` of the underlying function/action are applied.

Endpoint                     | Required permission scopes
-----------------------------|-----------------------
`GET Orders(1)/CalculateTax` | `Order.CalculateTax`
`POST UpdateTaxRate`         | `UpdateTaxRate`

**Note**: If functions are overloaded, the operation restrictions of the specific overload being called will apply.

### Operations on properties

The `ReadRestrictions` or `UpdateRestrictions` of the entity or singleton whose property are being accessed are applied.

Endpoint                                        | Restrictions applied
------------------------------------------------|----------------------
`GET Customers(1)/Address/City`                 | `Customers.Read OR Customers.ReadByKey`
`GET TopProduct/Price`                          | `TopProduct.Read`
`DELETE or PUT or POST Customers(1)/Email`      | `Customers.Update`

### Operations on navigation property links

These apply the `ReadRestrictions` and `UpdateRestrictions` of the entity/singleton that contains the navigation property where the link is read/added/removed/modified.

Endpoint                                            | Restrictions applied
----------------------------------------------------|----------------------
`GET Customers(1)/Orders/$ref`                      | `Customers.Read OR Customers.ReadByKey`
`GET TopCustomer/Orders/$ref`                       | `TopProduct.Read`
`DELETE or PUT or POST Customers(1)/Orders/$ref`    | `Customers.Update`

### Navigation properties

If the endpoint accesses a navigation properties and nested paths in general, the authorization middleware
will check whether the user has permissions to access each segment of the path.

Given the endpoint `GET Customers(1)/Orders`, the middleware will check whether the user
has read access to Customers(1) and then read access to Orders. The permissions that are checked for
reading Customers(1) are extracted from the `ReadRestrictions` (including `ReadByKeyRestrictions`) of
the `Customers` entity set. The permissions checked for Orders are extracted from both the `ReadRestrictions`
of `Orders` and the `ReadRestrictions` of the `NavigationRestrictions` of `Customers` that apply to the `Orders`
property (`NS.EntityContainer.Customers/{key}/Orders` path).

Assuming the model defines the scopes `Customers.Read`, `Customers.ReadByKey`, `CustomersOrders.Read` and `Orders.Read`,
the required scopes to read the endpoint would be:

```
(Customers.Read OR Customers.ReadByKey) AND (CustomerOrders.Read OR Orders.Read)
```

Endpoint                                    | Restrictions applied
--------------------------------------------|--------------------------
`GET Customers(1)/Orders`                   | `(Customers.Read OR Customers.ReadByKey) AND (CustomerOrders.Read OR Orders.Read))`
`GET Customers(1)/Orders(1)/Price`          | `(Customers.Read OR Customers.ReadByKey) AND (CustomerOrders.Read OR CustomerOrders.ReadByKey OR Orders.Read OR Orders.ReadByKey)`
`DELETE Customers(1)/Orders(1)`             | `(Customers.Update) AND (CustomerOrders.Delete OR Orders.Delete)`
`PUT Customers(1)/Orders(1)`                | `(Customers.Update) AND (CustomerOrders.Update or Orders.Update)`
`POST Customers(1)/Orders`                  | `(Customers.Update) AND (CustomerOrders.Insert or Orders.Insert)`
`GET Customers(1)/Orders(2)/Product`        | `(Customers.Read OR Customers.ReadByKey) AND (CustomerOrders.Read OR CustomerOrders.ReadByKey OR Orders.Read OR Orders.ReadByKey) AND (OrderProduct.Read OR OrderProduct.ReadByKey OR Products.Read)`

Note that a POST, PUT, PATCH or DELETE access to a navigation property is considered an update access to the entity that the navigation property belongs to.

## Limitations
- Only supports AspNetCore APIs using endpoing routing, i.e. AspNetCore 3.1
- Does not support [`RestrictedProperties`](https://github.com/oasis-tcs/odata-vocabularies/blob/master/vocabularies/Org.OData.Capabilities.V1.md#scopetype)
- Permissions are extracted from the model on each request, no caching is performed. It's not clear whether it's guaranteed that the model will not change after startup.
