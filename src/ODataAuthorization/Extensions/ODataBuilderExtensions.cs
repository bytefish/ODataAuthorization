// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.Contracts;

namespace ODataAuthorization.Extensions
{
    public static class ODataBuilderExtensions
    {
        /// <summary>
        /// Adds OData model-based authorization services
        /// </summary>
        /// <param name="odataBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddODataAuthorization(this IMvcBuilder builder)
        {
            Contract.Assert(builder != null);

            builder.Services.AddODataAuthorization();

            return builder;
        }

        /// <summary>
        /// Adds OData model-based authorization services
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureODataAuth">Action to configure the authorization options</param>
        /// <returns></returns>
        public static IMvcBuilder AddODataAuthorization(this IMvcBuilder builder, Action<ODataAuthorizationOptions> configureODataAuth)
        {
            Contract.Assert(builder != null);

            builder.Services.AddODataAuthorization(configureODataAuth);

            return builder;
        }
    }
}
