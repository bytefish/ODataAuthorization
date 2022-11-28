using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace ODataAuthorizationDemo.Models
{
    public static class AppEdmModel
    {
        public static IEdmModel GetModel()
        {
            var builder = new ODataConventionModelBuilder();

            var products = builder.EntitySet<Product>("Products");

            products.HasReadRestrictions()
                .HasPermissions(p =>
                    p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Product.Read")))
                .HasReadByKeyRestrictions(r => r.HasPermissions(p =>
                    p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Product.ReadByKey"))));

            products.HasInsertRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Product.Create")));

            products.HasUpdateRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Product.Update")));

            products.HasDeleteRestrictions()
                .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Product.Delete")));

            return builder.GetEdmModel();
        }
    }
}
