// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ODataAuthorization
{
    /// <summary>
    /// Evaluates whether specified scopes should be allow
    /// access to a restricted resource.
    /// </summary>
    internal interface IScopesEvaluator
    {
        /// <summary>
        /// Returns true if access should be granted based
        /// on the given <paramref name="scopes"/>.
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        bool AllowsScopes(IEnumerable<string> scopes);
    }
}
