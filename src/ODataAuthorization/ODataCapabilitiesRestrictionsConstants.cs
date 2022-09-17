// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace ODataAuthorization
{
    internal static class ODataCapabilityRestrictionsConstants
    {
        public const string CapabilitiesNamespace = "Org.OData.Capabilities.V1";
        public const string ReadRestrictions = $"{CapabilitiesNamespace}.ReadRestrictions";
        public const string ReadByKeyRestrictions = $"{CapabilitiesNamespace}.ReadByKeyRestrictions";
        public const string InsertRestrictions = $"{CapabilitiesNamespace}.InsertRestrictions";
        public const string UpdateRestrictions = $"{CapabilitiesNamespace}.UpdateRestrictions";
        public const string DeleteRestrictions = $"{CapabilitiesNamespace}.DeleteRestrictions";
        public const string OperationRestrictions = $"{CapabilitiesNamespace}.OperationRestrictions";
        public const string NavigationRestrictions = $"{CapabilitiesNamespace}.NavigationRestrictions";
    }
}
