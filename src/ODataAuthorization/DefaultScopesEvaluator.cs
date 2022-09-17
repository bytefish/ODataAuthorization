// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ODataAuthorization
{
    /// <summary>
    /// An <see cref="IScopesEvaluator"/> that always returns true.
    /// It's useful for operations that have no restrictions explicitly
    /// defined in the model. Such operations are assumed to be always
    /// allowed regardless of the user's scopes.
    /// </summary>
    internal class DefaultScopesEvaluator : IScopesEvaluator
    {
        public bool AllowsScopes(IEnumerable<string> scopes)
        {
            return true;
        }
    }
}
