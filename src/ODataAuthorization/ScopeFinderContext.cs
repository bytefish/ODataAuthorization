﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Security.Claims;

namespace ODataAuthorization
{
    /// <summary>
    /// Contains information used to extract permission scopes
    /// available to the authenticated user
    /// </summary>
    public class ScopeFinderContext
    {
        /// <summary>
        /// Creates an instance of <see cref="ScopeFinderContext"/>
        /// </summary>
        /// <param name="user">The authenticated user</param>
        public ScopeFinderContext(ClaimsPrincipal user)
        {
            User = user;
        }
        
        /// <summary>
        /// The <see cref="ClaimsPrincipal"/> representing the current user.
        /// </summary>
        public ClaimsPrincipal User { get; private set; }
    }
}
