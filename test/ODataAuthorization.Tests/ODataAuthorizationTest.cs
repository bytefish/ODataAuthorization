using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using ODataAuthorization.Tests.Extensions;
using ODataAuthorization.Tests.Models;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Authorization;
using System.Text.Encodings.Web;

namespace ODataAuthorization.Tests
{
    /// <summary>
    /// Controller feature provider
    /// </summary>
    public class WebODataControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>, IApplicationFeatureProvider
    {
        private Type[] _controllers;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebODataControllerFeatureProvider"/> class.
        /// </summary>
        /// <param name="controllers">The controllers</param>
        public WebODataControllerFeatureProvider(params Type[] controllers)
        {
            _controllers = controllers;
        }

        /// <summary>
        /// Updates the feature instance.
        /// </summary>
        /// <param name="parts">The list of <see cref="ApplicationPart" /> instances in the application.</param>
        /// <param name="feature">The controller feature.</param>
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            if (_controllers == null)
            {
                return;
            }

            feature.Controllers.Clear();
            foreach (var type in _controllers)
            {
                feature.Controllers.Add(type.GetTypeInfo());
            }
        }
    }

    /// <summary>
    /// Extension for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Config the controller provider.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="controllers">The configured controllers.</param>
        /// <returns>The caller.</returns>
        public static IServiceCollection ConfigureControllers(this IServiceCollection services, params Type[] controllers)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddControllers()
                .ConfigureApplicationPartManager(pm =>
                {
                    pm.FeatureProviders.Add(new WebODataControllerFeatureProvider(controllers));
                });

            return services;
        }
    }

    public class ODataAuthorizationTest
    {
        private readonly HttpClient _client;

        public ODataAuthorizationTest()
        {
            var server = CreateServer();

            _client = server.CreateClient();
        }

        private TestServer CreateServer()
        {
            var model = TestModel.GetModelWithPermissions();

            var controllers = new[]
            {
                typeof(ProductsController),
                typeof(MyProductController),
                typeof(RoutingCustomersController),
                typeof(VipCustomerController),
                typeof(SalesPeopleController),
                typeof(TodoItemController),
                typeof(IncidentsController),
                typeof(IncidentGroupsController)
            };

            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpContextAccessor();

                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowAll",
                            builder =>
                            {
                                builder
                                    .AllowAnyOrigin()
                                    .AllowAnyMethod()
                                    .AllowAnyHeader();
                            });
                    });

                    // Adds the Policy
                    services.AddAuthentication(a => a.AddScheme<CustomAuthHandler>("Test", "Test Auth"));
                    services.AddAuthorization(options => options.AddODataAuthorizationPolicy());

                    services
                        .AddControllers()
                        .AddOData((opt) =>
                        {
                            opt.RouteOptions.EnableActionNameCaseInsensitive = true;
                            opt.RouteOptions.EnableControllerNameCaseInsensitive = true;
                            opt.RouteOptions.EnablePropertyNameCaseInsensitive = true;
                            
                            opt.AddRouteComponents("odata", model)
                                .EnableQueryFeatures().Select().Expand().OrderBy().Filter().Count();
                        });

                    services.ConfigureControllers(controllers);
                })
                .Configure(app =>
                {
                    app.UseCors("AllowAll");
                    app.UseRouting();
                    
                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers()
                            .RequireAuthorization(ODataAuthorizationPolicies.Constants.DefaultPolicyName);
                    });
                });

            return new TestServer(builder);
        }


        [Theory]
        // GET /entityset
        [InlineData("GET", "Products", "Product.Read", "GET Products")]
        [InlineData("GET", "Products", "Product.ReadAll", "GET Products")]
        [InlineData("GET", "Products/$count", "Product.Read", "GET Products")]
        [InlineData("GET", "Products/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Read", "GET SpecialProducts")]
        [InlineData("GET", "Products/ODataAuthorization.Tests.Models.SpecialProduct/$count", "Product.Read", "GET SpecialProducts")]
        // POST /entityset
        [InlineData("POST", "Products", "Product.Insert", "POST Products")]
        [InlineData("POST", "Products/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Insert", "POST SpecialProduct")]
        // GET /entityset/key
        [InlineData("GET", "Products(10)", "Product.ReadByKey", "GET Products(10)")]
        [InlineData("GET", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct", "Product.ReadByKey", "GET SpecialProduct(10)")]
        // DELETE /entityset/key
        [InlineData("DELETE", "Products(10)", "Product.Delete", "DELETE Products(10)")]
        [InlineData("DELETE", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Delete", "DELETE SpecialProduct(10)")]
        // PUT /entityset/key
        [InlineData("PUT", "Products(10)", "Product.Update", "PUT Products(10)")]
        [InlineData("PUT", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Update", "PUT SpecialProduct(10)")]
        // PATCH /entityset/key
        [InlineData("PATCH", "Products(10)", "Product.Update", "PATCH Products(10)")]
        [InlineData("PATCH", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Update", "PATCH SpecialProduct(10)")]
        // /singleton and /singleton/cast
        [InlineData("GET", "MyProduct", "MyProduct.Read", "GET MyProduct")]
        [InlineData("GET", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct", "MyProduct.Read", "GET MySpecialProduct")]
        [InlineData("PUT", "MyProduct", "MyProduct.Update", "PUT MyProduct")]
        [InlineData("PUT", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct", "MyProduct.Update", "PUT MySpecialProduct")]
        [InlineData("PATCH", "MyProduct", "MyProduct.Update", "PATCH MyProduct")]
        [InlineData("PATCH", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct", "MyProduct.Update", "PATCH MySpecialProduct")]
        // bound functions
        [InlineData("GET", "Products(10)/FunctionBoundToProduct()", "Product.Function3", "FunctionBoundToProduct(10)")]
        [InlineData("GET", "Products(10)/FunctionBoundToProduct(P1=1)", "Product.Function3", "FunctionBoundToProduct(10, 1)")]
        [InlineData("GET", "Products(10)/FunctionBoundToProduct(P1=1,P2=2,P3='3')", "Product.Function3", "FunctionBoundToProduct(10, 1, 2, 3)")]
        // entityset functions
        [InlineData("GET", "Products/TopProductOfAll()", "Product.Top", "TopProductOfAll()")]
        [InlineData("GET", "Products/ODataAuthorization.Tests.Models.SpecialProduct/TopProductOfAll()", "Product.Top", "TopProductOfAll()")]
        // singleton functions
        [InlineData("GET", "MyProduct/FunctionBoundToProduct()", "Product.Function3", "FunctionBoundToProduct()")]
        // entity actions
        [InlineData("POST", "SalesPeople(10)/GetVIPRoutingCustomers", "SalesPerson.GetVip", "GetVIPRoutingCustomers(10)")]
        [InlineData("POST", "SalesPeople/GetVIPRoutingCustomers", "SalesPerson.GetVipOnCollection", "GetVIPRoutingCustomers()")]
        [InlineData("POST", "RoutingCustomers(10)/ODataAuthorization.Tests.Models.VIP/GetSalesPerson", "Customer.GetSalesPerson", "GetSalesPersonOnVIP(10)")]
        // entityset actions
        [InlineData("POST", "RoutingCustomers/GetProducts", "Customer.GetProducts", "GetProducts()")]
        [InlineData("POST", "RoutingCustomers/ODataAuthorization.Tests.Models.VIP/GetSalesPeople", "Customer.GetSalesPeople", "GetSalesPeopleOnVIP()")]
        // singleton actions
        [InlineData("POST", "VipCustomer/ODataAuthorization.Tests.Models.VIP/GetSalesPerson", "Customer.GetSalesPerson", "GetSalesPerson()")]
        [InlineData("POST", "VipCustomer/GetFavoriteProduct", "Customer.GetFavoriteProduct", "GetFavoriteProduct()")]
        // entityset/key/property
        [InlineData("GET", "Products(10)/Name", "Product.ReadByKey", "GetProductName(10)")]
        [InlineData("GET", "Products(10)/Name/$value", "Product.ReadByKey", "GetProductName(10)")]
        [InlineData("GET", "Products(10)/Tags/$count", "Product.ReadByKey", "GetProductTags(10)")]
        [InlineData("DELETE", "Products(10)/Name", "Product.Update", "DeleteProductName(10)")]
        [InlineData("PATCH", "Products(10)/Name", "Product.Update", "PatchProductName(10)")]
        [InlineData("PUT", "Products(10)/Name", "Product.Update", "PutProductName(10)")]
        [InlineData("POST", "Products(10)/Tags", "Product.Update", "PostProductTags(10)")]
        // entityset/key/cast/property
        [InlineData("GET", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Name", "Product.ReadByKey", "GetProductName(10)")]
        [InlineData("GET", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Name/$value", "Product.ReadByKey", "GetProductName(10)")]
        [InlineData("GET", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Tags/$count", "Product.ReadByKey", "GetProductTags(10)")]
        [InlineData("DELETE", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Name", "Product.Update", "DeleteProductName(10)")]
        [InlineData("PATCH", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Name", "Product.Update", "PatchProductName(10)")]
        [InlineData("PUT", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Name", "Product.Update", "PutProductName(10)")]
        [InlineData("POST", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/Tags", "Product.Update", "PostProductTags(10)")]
        // singleton/property
        [InlineData("GET", "MyProduct/Name", "MyProduct.Read", "GetMyProductName")]
        [InlineData("GET", "MyProduct/Name/$value", "MyProduct.Read", "GetMyProductName")]
        [InlineData("GET", "MyProduct/Tags/$count", "MyProduct.Read", "GetMyProductTags")]
        [InlineData("DELETE", "MyProduct/Name", "MyProduct.Update", "DeleteMyProductName")]
        [InlineData("PATCH", "MyProduct/Name", "MyProduct.Update", "PatchMyProductName")]
        [InlineData("PUT", "MyProduct/Name", "MyProduct.Update", "PutMyProductName")]
        [InlineData("POST", "MyProduct/Tags", "MyProduct.Update", "PostMyProductTags")]
        // singleton/cast/property
        [InlineData("GET", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Name/$value", "MyProduct.Read", "GetMyProductName")]
        [InlineData("GET", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Tags/$count", "MyProduct.Read", "GetMyProductTags")]
        [InlineData("DELETE", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Name", "MyProduct.Update", "DeleteMyProductName")]
        [InlineData("PATCH", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Name", "MyProduct.Update", "PatchMyProductName")]
        [InlineData("PUT", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Name", "MyProduct.Update", "PutMyProductName")]
        [InlineData("POST", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/Tags", "MyProduct.Update", "PostMyProductTags")]
        // navigation properties
        [InlineData("GET", "Products(10)/RoutingCustomers", "Product.ReadByKey,Customer.Read", "GetProductCustomers(10)")]
        [InlineData("POST", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/RoutingCustomers", "MyProduct.Update,Customer.Insert", "PostMyProductCustomer")]
        // $ref
        [InlineData("PUT", "Products(10)/RoutingCustomers/$ref", "Product.Update", "CreateProductCustomerRef(10)")]
        // unbound action
        [InlineData("POST", "GetRoutingCustomerById()", "GetRoutingCustomerById", "GetRoutingCustomerById")]
        // unbound function
        [InlineData("GET", "UnboundFunction()", "UnboundFunction", "UnboundFunction")]
        // complex routes requiring ODataRoute attribute
        [InlineData("GET", "Products(10)/RoutingCustomers(20)/Address/Street", "Product.Read,Customer.ReadByKey", "GetProductRoutingCustomerAddressStreet")]
        // dynamic properties
        [InlineData("GET", "SalesPeople(10)/SomeProperty", "SalesPerson.ReadByKey", "GetSalesPersonDynamicProperty(10, SomeProperty)")]
        [InlineData("GET", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/FunctionBoundToProduct()", "Product.Function3", "FunctionBoundToProduct()")]
        [InlineData("GET", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/FunctionBoundToProduct()", "Product.Function3", "FunctionBoundToProduct(10)")]
        // $expand
        [InlineData("GET", "Products?$expand=RoutingCustomers", "Product.ReadAll,ProductCustomers.Read", "GET Products")]
        [InlineData("GET", "Products?$expand=RoutingCustomers($expand=Products)", "Product.Read,ProductCustomers.Read,CustomerProducts.Read", "GET Products")]
        // TODO: Failing. Unclear Routing Conventions for $ref.
        [InlineData("GET", "Products(10)/RoutingCustomers(20)/$ref", "Product.ReadByKey", "GetProductCustomerRef(10, 20)", Skip = "Requires Unclear Routing")]
        [InlineData("POST", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/RoutingCustomers/$ref", "MyProduct.Update", "CreateMyProductCustomerRef", Skip = "Requires Unclear Routing")]
        [InlineData("DELETE", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct/RoutingCustomers(20)/$ref", "Product.Update", "DeleteProductCustomerRef(10, 20)", Skip = "Requires Unclear Routing")]
        [InlineData("DELETE", "MyProduct/RoutingCustomers(20)/$ref", "MyProduct.Update", "DeleteMyProductCustomerRef(20)", Skip = "Requires Unclear Routing")]
        // TODO: Failing. Method not allowed for MERGE.
        [InlineData("MERGE", "Products(10)", "Product.Update", "PATCH Products(10)", Skip = "Method Not Allowed")]
        [InlineData("MERGE", "Products(10)/ODataAuthorization.Tests.Models.SpecialProduct", "Product.Update", "PATCH SpecialProduct(10)", Skip = "Method Not Allowed")]
        [InlineData("MERGE", "MyProduct", "MyProduct.Update", "PATCH MyProduct", Skip = "Method Not Allowed")]
        [InlineData("MERGE", "MyProduct/ODataAuthorization.Tests.Models.SpecialProduct", "MyProduct.Update", "PATCH MySpecialProduct", Skip = "Method Not Allowed")]
        public async Task ShouldApplyModelPermissionsToEndpoints(string method, string endpoint, string permissions, string expectedResponse)
        {
            var uri = $"http://localhost/odata/{endpoint}";
            // permission forbidden if auth not provided
            HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(
                new HttpMethod(method), uri));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            // request succeeds if permission is correct
            var message = new HttpRequestMessage(new HttpMethod(method), uri);
            message.Headers.Add("Scopes", permissions);

            response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedResponse, response.Content.AsObjectContentValue());
        }

        [Theory]
        [InlineData("GET", "Incidents", "", "GetIncidents")]
        [InlineData("GET", "IncidentGroups(10)/Incidents", "IncidentGroup.Read", "GetIncidentGroupIncidents(10)")]
        public async Task ShouldGrantAccessIfModelDoesNotDefinePermissions(string method, string endpoint, string permissions, string expectedResponse)
        {
            var uri = $"http://localhost/odata/{endpoint}";
            var message = new HttpRequestMessage(new HttpMethod(method), uri);
            message.Headers.Add("Scopes", permissions);

            var response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedResponse, response.Content.AsObjectContentValue());
        }

        [Fact]
        public async Task ShouldIgnoreNonODataEndpoints()
        {
            var uri = "http://localhost/api/TodoItems";
            HttpResponseMessage  response = await _client.GetAsync(uri);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal("GET TodoItems", responseContent);


            var message = new HttpRequestMessage(new HttpMethod("GET"), uri);
            message.Headers.Add("Scopes", "Perm.Read");

            response = await _client.SendAsync(message);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GET TodoItems", response.Content.AsObjectContentValue());
        }
    }

    internal class CustomAuthHandler : AuthenticationHandler<CustomAuthOptions>
    {
        public CustomAuthHandler(IOptionsMonitor<CustomAuthOptions> options, ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder, TimeProvider timeProvider) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new System.Security.Principal.GenericIdentity("Me");
            var scopeValues = Request.Headers["Scopes"];
            if (scopeValues.Count != 0)
            {
                var scopes = scopeValues.ToArray()[0].Split(',');

                identity.AddClaims(scopes.Select(scope => new Claim(ODataAuthorizationPolicies.Constants.DefaultScopeClaimType, scope)));
            }

            var principal = new System.Security.Principal.GenericPrincipal(identity, Array.Empty<string>());
            var ticket = new AuthenticationTicket(principal, "AuthScheme");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    internal class CustomAuthOptions : AuthenticationSchemeOptions
    {
    }

    
    [ApiController]
    [Route("/api/TodoItems")]
    public class TodoItemController: Controller
    {
        [HttpGet]
        public string GetTodoItems()
        {
            return "GET TodoItems";
        }
    }

    public class ProductsController : ODataController
    {
        public string Get()
        {
            return "GET Products";
        }

        public string GetProductsFromSpecialProduct()
        {
            return "GET SpecialProducts";
        }

        public string Post()
        {
            return "POST Products";
        }

        public string PostFromSpecialProduct()
        {
            return "POST SpecialProduct";
        }

        public string Get(int key)
        {
            return $"GET Products({key})";
        }

        public string GetSpecialProduct(int key)
        {
            return $"GET SpecialProduct({key})";
        }

        public string Delete(int key)
        {
            return $"DELETE Products({key})";
        }

        public string DeleteSpecialProduct(int key)
        {
            return $"DELETE SpecialProduct({key})";
        }

        public string Put(int key)
        {
            return $"PUT Products({key})";
        }

        public string PutSpecialProduct(int key)
        {
            return $"PUT SpecialProduct({key})";
        }

        [HttpPatch]
        public IActionResult Patch([FromODataUri] int key)
        {
            return Ok($"PATCH Products({key})");
        }

        [HttpPatch("odata/Products({key})/ODataAuthorization.Tests.Models.SpecialProduct")]
        public string PatchFromSpecialProduct(int key)
        {
            return $"PATCH SpecialProduct({key})";
        }

        [HttpGet]
        [HttpGet("odata/Products({key})/ODataAuthorization.Tests.Models.SpecialProduct/FunctionBoundToProduct()")]
        public string FunctionBoundToProduct(int key)
        {
            return $"FunctionBoundToProduct({key})";
        }

        [HttpGet]
        public string FunctionBoundToProduct(int key, [FromODataUri] int P1)
        {
            return $"FunctionBoundToProduct({key}, {P1})";
        }
        
        [HttpGet]
        public string FunctionBoundToProduct(int key, [FromODataUri] int P1, [FromODataUri] int P2, [FromODataUri] string P3)
        {
            return $"FunctionBoundToProduct({key}, {P1}, {P2}, {P3})";
        }

        [EnableQuery]
        [HttpGet("FunctionBoundToProductOnSpecialProduct(key={key})")]
        public string FunctionBoundToProductOnSpecialProduct(int key)
        {
            return $"FunctionBoundToSpecialProduct({key})";
        }

        [HttpGet]
        [HttpGet("odata/Products/ODataAuthorization.Tests.Models.SpecialProduct/TopProductOfAll()")]
        public string TopProductOfAll()
        {
            return "TopProductOfAll()";
        }

        [HttpGet]
        public string TopProductOfAllFromSpecialProduct()
        {
            return "TopProductOfAll()";
        }


        public string GetName(int key)
        {
            return $"GetProductName({key})";
        }

        public string GetNameFromSpecialProduct(int key)
        {
            return $"GetProductName({key})";
        }

        public string PutToName(int key)
        {
            return $"PutProductName({key})";
        }

        public string PutToNameFromSpecialProduct(int key)
        {
            return $"PutProductName({key})";
        }

        public string PatchToName(int key)
        {
            return $"PatchProductName({key})";
        }
        public string PatchToNameFromSpecialProduct(int key)
        {
            return $"PatchProductName({key})";
        }

        public string DeleteToName(int key)
        {
            return $"DeleteProductName({key})";
        }

        [HttpDelete]
        public string DeleteToNameFromSpecialProduct(int key)
        {
            return $"DeleteProductName({key})";
        }

        public string PostToTags(int key)
        {
            return $"PostProductTags({key})";
        }
        public string PostToTagsFromSpecialProduct(int key)
        {
            return $"PostProductTags({key})";
        }

        public string GetTags(int key)
        {
            return $"GetProductTags({key})";
        }

        public string GetTagsFromSpecialProduct(int key)
        {
            return $"GetProductTags({key})";
        }

        [HttpPost("/odata/GetRoutingCustomers({key})")]
        public string GetRoutingCustomers(int key)
        {
            return $"GetProductCustomers({key})";
        }

        [HttpGet]
        [HttpGet("odata/Products({key})/RoutingCustomers({relatedKey})/$ref")]
        public string GetRefToRoutingCustomers(int key, int relatedKey)
        {
            return $"GetProductCustomerRef({key}, {relatedKey})";
        }

        public string DeleteRefToRoutingCustomers(int key, int relatedKey)
        {
            return $"DeleteProductCustomerRef({key}, {relatedKey})";
        }


        [HttpPost]
        public string CreateRefToRoutingCustomers(int key)
        {
            return $"CreateProductCustomerRef({key})";
        }

        [HttpPost]
        public string CreateRefFromSpecialProductToRoutingCustomers(int key)
        {
            return $"CreateProductCustomerRef({key})";
        }
        
        [HttpGet("odata/Products({key})/RoutingCustomers({relatedKey})/Address/Street")]
        public string GetProductRoutingCustomerAddressStreet([FromODataUri] int key, int relatedKey)
        {
            return "GetProductRoutingCustomerAddressStreet";
        }
    }

    public class MyProductController : ODataController
    {
        public string Get()
        {
            return "GET MyProduct";
        }

        public string GetFromSpecialProduct()
        {
            return "GET MySpecialProduct";
        }

        public string Put()
        {
            return "PUT MyProduct";
        }

        public string PutFromSpecialProduct()
        {
            return "PUT MySpecialProduct";
        }

        public string Patch()
        {
            return "PATCH MyProduct";
        }

        public string PatchFromSpecialProduct()
        {
            return "PATCH MySpecialProduct";
        }

        [HttpGet("FunctionBoundToProduct()")]
        [HttpGet("odata/MyProduct/ODataAuthorization.Tests.Models.SpecialProduct/FunctionBoundToProduct()")]
        public string FunctionBoundToProduct()
        {
            return "FunctionBoundToProduct()";
        }

        public string GetName()
        {
            return "GetMyProductName";
        }
        
        public string GetNameFromSpecialProduct()
        {
            return "GetMyProductName";
        }

        public string PutToName()
        {
            return "PutMyProductName";
        }

        public string PutToNameFromSpecialProduct()
        {
            return "PutMyProductName";
        }

        public string PatchToName()
        {
            return "PatchMyProductName";
        }
        
        public string PatchToNameFromSpecialProduct()
        {
            return "PatchMyProductName";
        }

        public string DeleteToName()
        {
            return "DeleteMyProductName";
        }
        
        public string DeleteToNameFromSpecialProduct()
        {
            return "DeleteMyProductName";
        }

        public string PostToTags()
        {
            return "PostMyProductTags";
        }

        public string PostToTagsFromSpecialProduct()
        {
            return "PostMyProductTags";
        }

        public string GetTags()
        {
            return "GetMyProductTags";
        }
        public string GetTagsFromSpecialProduct()
        {
            return "GetMyProductTags";
        }

        public string PostToRoutingCustomers()
        {
            return "PostMyProductCustomer";
        }

        public string PostToRoutingCustomersFromSpecialProduct()
        {
            return "PostMyProductCustomer";
        }

        public string CreateRefToRoutingCustomers()
        {
            return $"CreateMyProductCustomerRef";
        }

        public string CreateRefToRoutingCustomersFromSpecialProduct()
        {
            return $"CreateMyProductCustomerRef";
        }

        public string DeleteRefToRoutingCustomersFromSpecialProduct([FromODataUri] int relatedKey)
        {
            return $"DeleteMyProductCustomerRef({relatedKey})";
        }
    }

    public class RoutingCustomersController : ODataController
    {
        [HttpPost]
        public string GetProducts()
        {
            return "GetProducts()";
        }

        [HttpPost]
        public string GetSalesPersonOnVIP(int key)
        {
            return $"GetSalesPersonOnVIP({key})";
        }

        [HttpPost]
        public string GetSalesPeopleOnCollectionOfVIP()
        {
            return "GetSalesPeopleOnVIP()";
        }

        [HttpPost("odata/GetRoutingCustomerById()")]
        public string GetRoutingCustomerById()
        {
            return "GetRoutingCustomerById";
        }

        [HttpGet("odata/UnboundFunction()")]
        public IActionResult UnboundFunction()
        {
            return Ok("UnboundFunction");
        }
    }

    public class VipCustomerController : ODataController
    {
        [HttpPost]
        public string GetSalesPerson()
        {
            return "GetSalesPerson()";
        }

        [HttpPost]
        public string GetFavoriteProduct()
        {
            return "GetFavoriteProduct()";
        }

        public string GetName()
        {
            return "GetName()";
        }
    }

    public class SalesPeopleController : ODataController
    {
        [HttpPost]
        public string GetVIPRoutingCustomers(int key)
        {
            return $"GetVIPRoutingCustomers({key})";
        }

        [HttpPost]
        public string GetVIPRoutingCustomers()
        {
            return "GetVIPRoutingCustomers()";
        }

        [HttpGet("odata/SalesPeople({key})/{dynamicproperty}")]
        public string GetDynamicProperty(int key, string dynamicProperty)
        {
            return $"GetSalesPersonDynamicProperty({key}, {dynamicProperty})";
        }
    }

    public class IncidentsController : ODataController
    {
        public string Get()
        {
            return "GetIncidents";
        }
    }

    public class IncidentGroupsController : ODataController
    {
        [HttpGet("IncidentGroups({key})/Incidents")]
        public string GetIncidents(int key)
        {
            return $"GetIncidentGroupIncidents({key})";
        }
    }
}
