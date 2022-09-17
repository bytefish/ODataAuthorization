// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace ODataAuthorization
{
    /// <summary>
    /// Combines <see cref="IScopesEvaluator"/>s with a logical AND: returns true
    /// iff all evaluators return true or if evaluators are empty.
    /// </summary>
    internal class WithAndScopesCombiner: BaseScopesCombiner
    {

        public WithAndScopesCombiner(params IScopesEvaluator[] permissions) : base(permissions)
        { }

        public WithAndScopesCombiner(IEnumerable<IScopesEvaluator> permissions) : base(permissions)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            return Evaluators.All(permissions => permissions.AllowsScopes(scopes));
        }
    }
}
