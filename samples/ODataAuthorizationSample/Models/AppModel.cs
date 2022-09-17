
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.ModelBuilder;

namespace AspNetCore3ODataPermissionsSample.Models
{
    public class AppModel
    {
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            var customers = builder.EntitySet<Customer>("Customers");
            var orders = builder.EntitySet<Order>("Orders");
            var getTopCustomer = builder.Function("GetTopCustomer").ReturnsFromEntitySet<Customer>("Customers");

            var customerEntity = builder.EntityType<Customer>();
            var getAge = customerEntity.Function("GetAge").Returns<int>();

            // define permission restrictions
            customers.HasReadRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.Read")))
                .HasReadByKeyRestrictions(r => r.HasPermissions(p =>
                    p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.ReadByKey"))));

            customers.HasInsertRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.Insert")));

            customers.HasUpdateRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.Update")));

            customers.HasDeleteRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.Delete")));

            getTopCustomer.HasOperationRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.GetTop")));

            getAge.HasOperationRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.GetAge")));

            orders.HasReadRestrictions()
               .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Orders.Read")))
               .HasReadByKeyRestrictions(r => r.HasPermissions(p =>
                   p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Orders.ReadByKey"))));

            customers.HasNavigationRestrictions()
                .HasRestrictedProperties(props => props
                    .HasNavigationProperty(new EdmNavigationPropertyPathExpression("Customers/Orders"))
                    .HasReadRestrictions(r => r
                        .HasPermissions(p => p.HasSchemeName("Sheme").HasScopes(s => s.HasScope("Customers.ReadOrders")))
                        .HasReadByKeyRestrictions(r => r.HasPermissions(p =>
                            p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.ReadOrderByKey"))))))
                .HasRestrictedProperties(props => props
                    .HasNavigationProperty(new EdmNavigationPropertyPathExpression("Customers/Order"))
                    .HasReadRestrictions(r => r
                        .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Customers.ReadOrder")))));


            var model = builder.GetEdmModel();

            return model;
        }
    }
}
