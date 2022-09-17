// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODataAuthorization
{
    /// <summary>
    /// Provides configuration for the OData authorization layer
    /// </summary>
    public class ODataAuthorizationOptions
    {
        IServiceCollection _services;


        public ODataAuthorizationOptions(IServiceCollection services)
        {
            _services = services;
        }

        public bool AuthenticationConfigured { private set; get; }
        public bool AuthorizationConfigured { private set; get; }

        /// <summary>
        /// Gets or sets the delegate used to find the scopes granted to the authenticated user
        /// from the authorization context.
        /// By default the library tries to get scopes from the principal's claims that have "Scope" as the key.
        /// </summary>
        public Func<ScopeFinderContext, Task<IEnumerable<string>>> ScopesFinder { get; set; }

        public AuthenticationBuilder ConfigureAuthentication()
        {
            AuthenticationConfigured = true;
            return _services.AddAuthentication();
        }

        public AuthenticationBuilder ConfigureAuthentication(string defaultScheme)
        {
            AuthenticationConfigured = true;
            return _services.AddAuthentication(defaultScheme);
        }

        public AuthenticationBuilder ConfigureAuthentication(Action<AuthenticationOptions> configureOptions)
        {
            AuthenticationConfigured = true;
            return _services.AddAuthentication(configureOptions);
        }

        public void ConfigureAuthorization()
        {
            AuthorizationConfigured = true;
            _services.AddAuthorization();
        }

        public void ConfigureAuthorization(Action<AuthorizationOptions> configure)
        {
            AuthorizationConfigured = true;
            _services.AddAuthorization(configure);
        }
    }
}
