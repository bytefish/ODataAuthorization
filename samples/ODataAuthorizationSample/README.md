# OData WebApi Authorization sample

This application demonstrates how to use the OData authorization extensions to apply permissions to OData endpoints based on the model capability restrictions.

The application defines a model with CRUD permission restrictions annotations on the `Customers` and `Orders` entity sets, the
`GetTopCustomer` unbound function and `GetAge` bound function.

It uses a custom authentication handler that assumes a
user is always authenticated. This handler extracts the permissions from a header called `Permissions`, which
is a comma-separated list of allowed scopes.

Based on the model annotations, the:

| Endpoint                 | Required permissions
---------------------------|----------------------
`GET /odata/Customers`     | `Customers.Read`
`GET /odata/Customers/1`   | `Customers.Read` or `Customers.ReadByKey`
`DELETE /odata/Customers/1`| `Customers.Delete`
`POST /odata/Customers`    | `Customers.Insert`
`GET /odata/Customers(1)/GetAge` | `Customers.GetAge`
`GET /odata/GetTopCustomer`| `Customers.GetTop`
`GET /odata/Customers(1)/Orders` | (`Customers.Read` or `Customers.ReadByKey`) and (`Orders.Read` or `Customers.ReadOrders`)
`GET /odata/Customers(1)/Orders(1)` | `(Customers.Read` or `Customers.ReadByKey`) and (`Orders.Read` or `Orders.ReadByKey` or `Customers.Read` or `Customer.ReadByKey`)
`GET /odata/Customers(1)/Orders(1)/Title` | `(Customers.Read` or `Customers.ReadByKey`) and (`Orders.Read` or `Orders.ReadByKey` or `Customers.Read` or `Customer.ReadByKey`)
`GET /odata/Customers(1)/Orders/(1)/$ref` | `(Customers.Read` or `Customers.ReadByKey`)
`GET /odata/Customers(1)/Order` | (`Customers.Read` or `Customers.ReadByKey`) and (`Customers.ReadOrder` or `Orders.Read`)
`GET /odata/Customers(1)/Order/Title` | (`Customers.Read` or `Customers.ReadByKey`) and (`Customers.ReadOrder` or `Orders.Read`)

To test the app, run it and open Postman. In Postman
add a header called `Permissions` and any of the permissions
specified above in a comma-separated list (e.g. `Customers.Read, Customers.Insert`), then make requests to the endpoints above. If you hit an endpoint without adding its required permissions to the header, you should get a `403 Forbidden` error.
