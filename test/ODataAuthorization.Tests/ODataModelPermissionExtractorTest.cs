using ODataAuthorization.Tests.Models;
using Microsoft.AspNetCore.OData.Routing.Parser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using Xunit;
using Microsoft.AspNetCore.OData;

namespace ODataAuthorization.Tests
{
    public class ODataModelPermissionExtractorTest
    {

        IEdmModel _model = TestModel.GetModelWithPermissions();
        string _serviceRoot = "http://odata/";
        DefaultODataPathTemplateParser _parser = new DefaultODataPathTemplateParser();
        IServiceProvider _serviceProvider;

        public ODataModelPermissionExtractorTest()
        {
            _serviceProvider = CreateServiceProvider(_model);
        }

        private static IServiceProvider CreateServiceProvider(IEdmModel model)
        {
            var container = new ServiceCollection();
            container.AddSingleton(model);
            container.AddSingleton<ODataUriResolver>();
            container.AddSingleton<ODataSimplifiedOptions>();
            container.AddSingleton<ODataUriParserSettings>();
            container.AddSingleton<UriPathParser>();
            container.AddSingleton<ODataOptions>();
            return container.BuildServiceProvider();
        }

        [Theory]
        // Entity set CRUD
        [InlineData("GET", "Products", "Product.ReadAll")]
        [InlineData("GET", "Products(1)", "Product.ReadAll")]
        [InlineData("GET", "Products(1)", "Product.ReadByKey")]
        [InlineData("PUT", "Products(1)", "Product.Update")]
        [InlineData("PATCH", "Products(1)", "Product.Update")]
        [InlineData("MERGE", "Products(1)", "Product.Update")]
        [InlineData("DELETE", "Products(1)", "Product.Delete")]
        [InlineData("POST", "Products", "Product.Insert")]
        // Singleton CRUD
        [InlineData("GET", "MyProduct", "MyProduct.Read")]
        [InlineData("PUT", "MyProduct", "MyProduct.Update")]
        [InlineData("PATCH", "MyProduct", "MyProduct.Update")]
        [InlineData("MERGE", "MyProduct", "MyProduct.Update")]
        [InlineData("DELETE", "MyProduct", "MyProduct.Delete")]
        // Property access
        [InlineData("GET", "Products(10)/Name", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/Name/$value", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/Tags/$count", "Product.ReadByKey")]
        [InlineData("DELETE", "Products(10)/Name", "Product.Update")]
        [InlineData("PATCH", "Products(10)/Name", "Product.Update")]
        [InlineData("PUT", "Products(10)/Name", "Product.Update")]
        [InlineData("POST", "Products(10)/Tags", "Product.Update")]
        [InlineData("GET", "MyProduct/Name", "MyProduct.Read")]
        [InlineData("PUT", "MyProduct/Name", "MyProduct.Update")]
        [InlineData("PATCH", "MyProduct/Name", "MyProduct.Update")]
        [InlineData("DELETE", "MyProduct/Name", "MyProduct.Update")]
        // Navigation Properties
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.ReadByKey,Customer.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.Read,ProductCustomers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.Read,ProductCustomers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.Read,ProductCustomers.ReadByKey")]
        [InlineData("GET", "Products(10)/RoutingCustomers(10)", "Product.ReadByKey,Customer.ReadByKey")]
        [InlineData("POST", "Products(10)/RoutingCustomers", "Product.Update,Customer.Insert")]
        [InlineData("POST", "Products(10)/RoutingCustomers", "Product.Update,ProductCustomers.Insert")]
        [InlineData("PUT", "Products(10)/RoutingCustomers(10)", "Product.Update,Customer.Update")]
        [InlineData("PUT", "Products(10)/RoutingCustomers(10)", "Product.Update,ProductCustomers.Update")]
        [InlineData("DELETE", "Products(10)/RoutingCustomers(10)", "Product.Update,Customer.Delete")]
        [InlineData("DELETE", "Products(10)/RoutingCustomers(10)", "Product.Update,ProductCustomers.Delete")]
        public void PermissionEvaluator_ReturnsTrue_IfScopesMatchRequiredPermissions(string method, string endpoint, string userScopes)
        {
            var path = Parse(_model, new Uri(_serviceRoot), new Uri(endpoint, UriKind.Relative), _serviceProvider);

            var scopesList = userScopes.Split(',');

            var permissionHandler = _model.ExtractPermissionsForRequest(method, path, null);

            Assert.True(permissionHandler.AllowsScopes(scopesList));
        }

        [Theory]
        [InlineData("GET", "Products", "")]
        [InlineData("GET", "Products", "Customers.Read")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.ReadByKey")]
        [InlineData("GET", "Products(10)/RoutingCustomers", "ProductCustomers.Read")]
        public void PermissionEvaluator_ReturnsFalse_IfRequiredScopesNotFound(string method, string endpoint, string userScopes)
        {
            var path = Parse(_model, new Uri(_serviceRoot), new Uri(endpoint, UriKind.Relative), _serviceProvider);

            var scopesList = userScopes.Split(',');
            
            var permissionHandler = _model.ExtractPermissionsForRequest(method, path, null);

            Assert.False(permissionHandler.AllowsScopes(scopesList));
        }

        /// <inheritdoc />
        public virtual ODataPath Parse(IEdmModel model, Uri serviceRoot, Uri odataPath, IServiceProvider requestProvider)
        {
            ODataUriParser uriParser;
            if (serviceRoot != null)
            {
                uriParser = new ODataUriParser(model, serviceRoot, odataPath, requestProvider);
            }
            else
            {
                uriParser = new ODataUriParser(model, odataPath, requestProvider);
            }

            uriParser.Resolver = uriParser.Resolver ?? new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };
            uriParser.UrlKeyDelimiter = ODataUrlKeyDelimiter.Slash; // support key in parentheses and key as segment.

            // The ParsePath throws OData exceptions if the odata path is not valid.
            // That's expected.
            return uriParser.ParsePath();
        }
    }
}
