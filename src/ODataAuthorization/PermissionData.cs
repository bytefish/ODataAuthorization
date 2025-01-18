// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace ODataAuthorization
{
    /// <summary>
    /// Represents permission restrictions extracted from an OData model.
    /// </summary>
    internal class PermissionData: IScopesEvaluator
    {
        public required string SchemeName { get; set; }

        public IList<PermissionScopeData> Scopes { get; set; } = [];

        public bool AllowsScopes(IEnumerable<string> scopes)
        {
            var allowedScopes = Scopes.Select(s => s.Scope);

            return allowedScopes.Intersect(scopes).Any();
        }
    }

    internal class PermissionScopeData
    {
        public required string Scope { get; set; }

        public required string? RestrictedProperties { get; set; }
    }
}
