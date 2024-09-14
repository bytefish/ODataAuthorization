// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.ModelBuilder;

namespace JwtAuthenticationSample.Models;

public class AppEdmModel
{
    public static IEdmModel GetModel()
    {
        var builder = new ODataConventionModelBuilder();

        var products = builder.EntitySet<Product>("Products");
        var addresses = builder.EntitySet<Address>("Addresses");

        products.HasReadRestrictions()
            .HasPermissions(p =>
                p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.Read")))
            .HasReadByKeyRestrictions(r => r.HasPermissions(p =>
                p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.ReadByKey"))));

        products.HasInsertRestrictions()
            .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.Create")));

        products.HasUpdateRestrictions()
            .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.Update")));

        products.HasDeleteRestrictions()
            .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.Delete")));


        // TODO: I want to add a permission to control expand address from product
        products.HasNavigationRestrictions()
                      .HasRestrictedProperties(props => props
                          .HasNavigationProperty(new EdmNavigationPropertyPathExpression("Products/Address"))
                          .HasReadRestrictions(r => r
                              .HasPermissions(p => p.HasSchemeName("Scheme").HasScopes(s => s.HasScope("Products.ReadAddress")))));


        return builder.GetEdmModel();
    }
}