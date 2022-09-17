// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace ODataAuthorization
{
    /// <summary>
    /// Combines <see cref="IScopesEvaluator"/>s using a logical OR: returns
    /// true if any of the evaluators return true or if there
    /// are no evualtors added to the combiner.
    /// </summary>
    internal class WithOrScopesCombiner: BaseScopesCombiner
    {
        public WithOrScopesCombiner(params IScopesEvaluator[] evaluators) : base(evaluators)
        { }

        public WithOrScopesCombiner(IEnumerable<IScopesEvaluator> evaluators) : base(evaluators)
        { }

        public override bool AllowsScopes(IEnumerable<string> scopes)
        {
            if (!Evaluators.Any())
            {
                return true;
            }

            return Evaluators.Any(permission => permission.AllowsScopes(scopes));
        }
    }
}
