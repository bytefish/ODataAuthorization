using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ODataAuthorization
{
    public static class ODataAuthorizationPolicies
    {
        public static class Constants
        {
            /// <summary>
            /// Gets the Default Policy Name.
            /// </summary>
            public const string DefaultPolicyName = "OData";

            /// <summary>
            /// Gets the Default Scope Claim Type.
            /// </summary>
            public const string DefaultScopeClaimType = "Scope";
        }

        public static void AddODataAuthorizationPolicy(this AuthorizationOptions options)
        {
            options.AddODataAuthorizationPolicy(Constants.DefaultPolicyName);
        }

        public static void AddODataAuthorizationPolicy(this AuthorizationOptions options, string name)
        {
            Func<ClaimsPrincipal, IEnumerable<string>> getUserScopes = (user) => user
                            .FindAll(Constants.DefaultScopeClaimType)
                            .Select(claim => claim.Value);

            options.AddODataAuthorizationPolicy(name, getUserScopes);
        }

        public static void AddODataAuthorizationPolicy(this AuthorizationOptions options, string name, Func<ClaimsPrincipal, IEnumerable<string>> getUserScopes)
        {
            options.AddPolicy(name, policyBuilder =>
            {
                policyBuilder.RequireAssertion((ctx) =>
                {
                    var resource = ctx.Resource;

                    // We can only work on a HttpContext or we are out
                    if (resource is not HttpContext httpContext)
                    {
                        return false;
                    }

                    // Get all Scopes for the User
                    var scopes = getUserScopes(httpContext.User);

                    bool isAccessAllowed = IsAccessAllowed(httpContext, scopes);

                    return isAccessAllowed;
                });
            });
        }

        /// <summary>
        /// Checks if the Access to the requested Resource is allowed based on the Scopes.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the OData Route</param>
        /// <param name="scopes">List of Scopes to check against the Model Permissions</param>
        /// <returns></returns>
        public static bool IsAccessAllowed(HttpContext httpContext, IEnumerable<string> scopes)
        {
            // Get the OData Feature to access the parsed OData components
            var odataFeature = httpContext.ODataFeature();

            if (odataFeature == null || odataFeature.Path == null)
            {
                return false;
            }

            // Get the EDM Model associated with the Request
            IEdmModel model = httpContext.Request.GetModel();

            if (model == null)
            {
                return false;
            }

            // At this point in the Middleware the SelectExpandClause hasn't been evaluated yet (https://github.com/OData/WebApiAuthorization/issues/4),
            // but it's needed to provide securing the $expand-statements, so that you can't request expanded data without the required permissions.
            ParseSelectExpandClause(httpContext, model, odataFeature);

            // Extract the Required Permissions for the Request using the 
            var permissions = ODataModelPermissionsExtractor.ExtractPermissionsForRequest(model, httpContext.Request.Method, odataFeature.Path, odataFeature.SelectExpandClause);

            // Evaluate the Scopes
            bool allowsScope = permissions.AllowsScopes(scopes);

            return allowsScope;
        }

        private static void ParseSelectExpandClause(HttpContext httpContext, IEdmModel model, IODataFeature odataFeature)
        {
            if (odataFeature == null)
            {
                return;
            }

            if (odataFeature.SelectExpandClause != null)
            {
                return;
            }

            try
            {
                var elementType = odataFeature.Path.LastOrDefault(x => x.EdmType != null);

                if (elementType != null)
                {
                    var queryOptions = new ODataQueryOptions(new ODataQueryContext(model, elementType.EdmType.AsElementType(), odataFeature.Path), httpContext.Request);

                    odataFeature.SelectExpandClause = queryOptions.SelectExpand?.SelectExpandClause;
                }
            }
            catch (Exception e)
            {
                httpContext.RequestServices
                    .GetRequiredService<ILogger>()
                    .LogInformation(e, "Failed to parse SelectExpandClause");
            }
        }

    }
}
