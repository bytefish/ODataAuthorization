# OData WebApi Authorization Cookie authentication demo

This application demonstrates how to use OData WebApi Authorization with a cookie-based authentication scheme.

The OData API has a single entity set `Products` and supports the basic CRUD requests. The model
has been annotated with permission restrictions for these CRUD operations:


| Endpoint                 | Required permissions
---------------------------|----------------------
`GET /odata/Products`     | `Product.Read`
`GET /odata/Products/1`   | `Product.Read` or `Product.ReadByKey`
`DELETE /odata/Products/1`| `Product.Delete`
`POST /odata/Products`    | `Product.Insert`
`PATCH /odata/Products(1)` | `Product.Update`

In order to access an endpoint, you will need to authenticate with the right scopes.

An `POST /auth/login` endpoint exists to allow you to authenticate yourself and specify the scopes you want the authenticated user to have.

For example, in order to authenticate with read and insert permissions on the `Products` entity set:

```
POST /auth/login
```
Body:
```json
{
    "RequestedScopes": ["Product.Insert", "Product.Read"]
}
```

To logout, make the following request:

```
POST /auth/logout
```

When you access an endpoint without the right permissions, it might return a `404` error instead of the expected `403`. This is because
the cookie authentication handler tries to redirect to a login page by default. No such page exists in this demo, hence the `404` error.
