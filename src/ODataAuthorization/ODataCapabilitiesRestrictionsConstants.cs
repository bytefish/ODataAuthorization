// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace ODataAuthorization
{
    internal static class ODataCapabilityRestrictionsConstants
    {
        /// <summary>
        /// Gets the Capabilities namespace.
        /// </summary>
        public const string CapabilitiesNamespace = "Org.OData.Capabilities.V1";

        /// <summary>
        /// Gets the ReadRestrictions term.
        /// </summary>
        public const string ReadRestrictions = $"{CapabilitiesNamespace}.ReadRestrictions";

        /// <summary>
        /// Gets the ReadByKeyRestrictions term.
        /// </summary>
        public const string ReadByKeyRestrictions = $"{CapabilitiesNamespace}.ReadByKeyRestrictions";

        /// <summary>
        /// Gets the InsertRestrictions term.
        /// </summary>
        public const string InsertRestrictions = $"{CapabilitiesNamespace}.InsertRestrictions";

        /// <summary>
        /// Gets the UpdateRestrictions term.
        /// </summary>
        public const string UpdateRestrictions = $"{CapabilitiesNamespace}.UpdateRestrictions";

        /// <summary>
        /// Gets the DeleteRestrictions term.
        /// </summary>
        public const string DeleteRestrictions = $"{CapabilitiesNamespace}.DeleteRestrictions";

        /// <summary>
        /// Gets the OperationRestrictions term.
        /// </summary>
        public const string OperationRestrictions = $"{CapabilitiesNamespace}.OperationRestrictions";

        /// <summary>
        /// Gets the NavigationRestrictions term.
        /// </summary>
        public const string NavigationRestrictions = $"{CapabilitiesNamespace}.NavigationRestrictions";
    }
}
