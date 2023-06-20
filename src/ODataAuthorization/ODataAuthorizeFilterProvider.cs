// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using System;
using System.Linq;

namespace ODataAuthorization
{
    public class ODataAuthorizeFilterProvider : IFilterProvider
    {
        public int Order => 0;

        public void OnProvidersExecuted(FilterProviderContext context)
        {
            
        }

        public void OnProvidersExecuting(FilterProviderContext context)
        {
            var httpContext = context.ActionContext.HttpContext;

            var odataFeature = httpContext.ODataFeature();

            if (odataFeature == null || odataFeature.Path == null)
            {
                return;
            }

            IEdmModel model = httpContext.Request.GetModel();

            if (model == null)
            {
                return;
            }

            // At this point in the Middleware the SelectExpandClause hasn't been evaluated (https://github.com/OData/WebApiAuthorization/issues/4),
            // but it's needed to provide securing $expand-statements, so you can't request expanded data without permissions.
            ParseSelectExpandClause(httpContext, model, odataFeature);

            var permissions = model.ExtractPermissionsForRequest(httpContext.Request.Method, odataFeature.Path, odataFeature.SelectExpandClause);

            var requirement = new ODataAuthorizationScopesRequirement(permissions);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(requirement)
                .Build();

            // We use the AuthorizeFilter instead of relying on the built-in authorization middleware
            // because we cannot add new metadata to the endpoint in the middle of a request
            // and OData's current implementation of endpoint routing does not allow for
            // adding metadata to individual routes ahead of time
            var authFilter = new AuthorizeFilter(policy);
            
            var authFilterDescriptor = new FilterDescriptor(authFilter, FilterScope.Global);

            var authFilterItem = new FilterItem(authFilterDescriptor, authFilter)
            {
                IsReusable = false
            };

            context.Results.Add(authFilterItem);
        }


        private void ParseSelectExpandClause(HttpContext httpContext, IEdmModel model, IODataFeature odataFeature)
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
                //_logger.LogInformation(e, "Failed to parse SelectExpandClause");
            }
        }
    }
}
