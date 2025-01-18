// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.UriParser;

namespace ODataAuthorization
{
    public static class ODataModelPermissionsExtractor
    {
        public static IScopesEvaluator ExtractPermissionsForRequest(IEdmModel model, string method, ODataPath odataPath, SelectExpandClause selectExpandClause)
        {
            ODataPathSegment prevSegment = null;

            var segments = new List<ODataPathSegment>();

            // this combines the permission scopes across path segments
            // with a logical AND
            var permissionsChain = new WithAndScopesCombiner();

            var lastSegmentIndex = odataPath.Count - 1;

            if (odataPath.ToString().EndsWith("$ref", StringComparison.OrdinalIgnoreCase))
            {
                // For ref segments, we apply the permission of the entity that contains the navigation propertye.g. for GET Customers(10)/Products/$ref,
                // we apply the read key permissions of Customers. For GET TopCustomer/Products/$ref, we apply the read permissions of TopCustomer. For
                // DELETE Customers(10)/Products(10)/$ref we apply the update permissions of Customers.
                lastSegmentIndex = odataPath.Count - 2;

                while (!(odataPath.ElementAt(lastSegmentIndex) is KeySegment || odataPath.ElementAt(lastSegmentIndex) is SingletonSegment || odataPath.ElementAt(lastSegmentIndex) is NavigationPropertySegment) && lastSegmentIndex > 0)
                {
                    lastSegmentIndex--;
                }
            }

            for (int i = 0; i <= lastSegmentIndex; i++)
            {
                var segment = odataPath.ElementAt(i);
                
                if (segment is EntitySetSegment ||
                        segment is SingletonSegment ||
                        segment is NavigationPropertySegment ||
                        segment is OperationSegment ||
                        segment is OperationImportSegment ||
                        segment is KeySegment ||
                        segment is PropertySegment)
                {
                    var parent = prevSegment;
                    var isPropertyAccess = IsNextSegmentOfType<PropertySegment>(odataPath, i) ||
                        IsNextSegmentOfType<NavigationPropertyLinkSegment>(odataPath, i) ||
                        IsNextSegmentOfType<NavigationPropertySegment>(odataPath, i);
                    prevSegment = segment;
                    segments.Add(segment);

                    if (segment is EntitySetSegment entitySetSegment)
                    {
                        // if Customers(key), then we'll handle it when we reach the key segment
                        // so that we can properly handle ReadByKeyRestrictions
                        if (IsNextSegmentKey(odataPath, i))
                        {
                            continue;
                        }

                        // if Customers/UnboundFunction, then we'll handle it when we reach the operation segment
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        IScopesEvaluator permissions;
                        
                        permissions = GetNavigationPropertyCrudPermisions(
                            segments,
                            false,
                            model,
                            method);

                        if (permissions is DefaultScopesEvaluator)
                        {
                            permissions = GetNavigationSourceCrudPermissions(entitySetSegment.EntitySet, model, method);
                        }

                        var handler = new WithOrScopesCombiner(permissions);
                        permissionsChain.Add(handler);
                    }
                    else if (segment is SingletonSegment singletonSegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach the operation segment
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        if (isPropertyAccess)
                        {
                            var propertyPermissions = GetSingletonPropertyOperationPermissions(singletonSegment.Singleton, model, method);
                            permissionsChain.Add(new WithOrScopesCombiner(propertyPermissions));
                        }
                        else
                        {
                            var permissions = GetNavigationSourceCrudPermissions(singletonSegment.Singleton, model, method);
                            permissionsChain.Add(new WithOrScopesCombiner(permissions));
                        }
                    }
                    else if (segment is KeySegment keySegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach the operation segment
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        var entitySet = keySegment.NavigationSource as IEdmEntitySet;
                        var permissions =  isPropertyAccess ?
                            GetEntityPropertyOperationPermissions(entitySet, model, method) :
                            GetEntityCrudPermissions(entitySet, model, method);

                        var evaluator = new WithOrScopesCombiner(permissions);


                        if (parent is NavigationPropertySegment)
                        {
                            var nestedPermissions = isPropertyAccess ?
                                GetNavigationPropertyPropertyOperationPermisions(segments, isTargetByKey: true, model, method) :
                                GetNavigationPropertyCrudPermisions(segments, isTargetByKey: true, model, method);

                            evaluator.Add(nestedPermissions);
                        }
                        

                        permissionsChain.Add(evaluator);
                    }
                    else if (segment is NavigationPropertySegment navSegment)
                    {
                        // if Customers/UnboundFunction, then we'll handle it when we reach there
                        if (IsNextSegmentOfType<OperationSegment>(odataPath, i))
                        {
                            continue;
                        }

                        // if Customers(key), then we'll handle it when we reach the key segment
                        // so that we can properly handle ReadByKeyRestrictions
                        if (IsNextSegmentKey(odataPath, i))
                        {
                            continue;
                        }

                        var topLevelPermissions = GetNavigationSourceCrudPermissions(navSegment.NavigationSource as IEdmVocabularyAnnotatable, model, method);
                        var segmentEvaluator = new WithOrScopesCombiner(topLevelPermissions);

                        var nestedPermissions = GetNavigationPropertyCrudPermisions(
                            segments,
                            isTargetByKey: false,
                            model,
                            method);

                        
                        segmentEvaluator.Add(nestedPermissions);
                        permissionsChain.Add(segmentEvaluator);
                    }
                    else if (segment is OperationImportSegment operationImportSegment)
                    {
                        var annotations = operationImportSegment.OperationImports.First().Operation.VocabularyAnnotations(model);
                        var permissions = GetOperationPermissions(annotations);
                        permissionsChain.Add(new WithOrScopesCombiner(permissions));
                    }
                    else if (segment is OperationSegment operationSegment)
                    {
                        var annotations = operationSegment.Operations.First().VocabularyAnnotations(model);
                        var operationPermissions = GetOperationPermissions(annotations);
                        permissionsChain.Add(new WithOrScopesCombiner(operationPermissions));
                    }
                }
            }


            // add permission for expanded properties
            var expandNavigationPaths = ExtractExpandedNavigationProperties(selectExpandClause, segments);

            foreach (var expandNavigationPath in expandNavigationPaths)
            {
                var navigationPathList = expandNavigationPath.ToList();
                var expandedPathEvaluator = GetNavigationPropertyReadPermissions(navigationPathList, navigationPathList.OfType<KeySegment>().Any(), model);
                permissionsChain.Add(new WithOrScopesCombiner(expandedPathEvaluator));
            }

            return permissionsChain;
        }

        private static IScopesEvaluator GetNavigationPropertyCrudPermisions(IList<ODataPathSegment> pathSegments, bool isTargetByKey, IEdmModel model, string method)
        {
            if (pathSegments.Count <= 1)
            {
                return new DefaultScopesEvaluator();
            }

            var expectedPath = GetPathFromSegments(pathSegments);
            IEdmVocabularyAnnotatable root = (pathSegments[0] as EntitySetSegment)?.EntitySet as IEdmVocabularyAnnotatable ??
                (pathSegments[0] as SingletonSegment)?.Singleton;

            var navRestrictions = root.VocabularyAnnotations(model).Where(a => a.Term.FullName() == ODataCapabilityRestrictionsConstants.NavigationRestrictions);
            foreach (var restriction in navRestrictions)
            {
                if (restriction.Value is IEdmRecordExpression record)
                {
                    var temp = record.FindProperty("RestrictedProperties");
                    if (temp?.Value is IEdmCollectionExpression restrictedProperties)
                    {
                        foreach (var item in restrictedProperties.Elements)
                        {
                            if (item is IEdmRecordExpression restrictedProperty)
                            {
                                var navigationProperty = restrictedProperty.FindProperty("NavigationProperty").Value as IEdmPathExpression;
                                if (navigationProperty?.Path == expectedPath)
                                
                                {
                                    if (method == "GET")
                                    {
                                        var readRestrictions = restrictedProperty.FindProperty("ReadRestrictions")?.Value as IEdmRecordExpression;
                                        var readPermissions = ExtractPermissionsFromRecord(readRestrictions);
                                        var evaluator = new WithOrScopesCombiner(readPermissions);
                                        
                                        if (isTargetByKey)
                                        {
                                            var readByKeyRestrictions = readRestrictions.FindProperty("ReadByKeyRestrictions")?.Value as IEdmRecordExpression;
                                            var readByKeyPermissions =  ExtractPermissionsFromRecord(readByKeyRestrictions);
                                            evaluator.AddRange(readByKeyPermissions);
                                        }

                                        return evaluator;
                                    }
                                    else if (method == "POST")
                                    {
                                        var insertRestrictions = restrictedProperty.FindProperty("InsertRestrictions")?.Value as IEdmRecordExpression;
                                        var insertPermissions = ExtractPermissionsFromRecord(insertRestrictions);
                                        return new WithOrScopesCombiner(insertPermissions);
                                    }
                                    else if (method == "PATCH" || method == "PUT" || method == "PATCH")
                                    {
                                        var updateRestrictions = restrictedProperty.FindProperty("UpdateRestrictions")?.Value as IEdmRecordExpression;
                                        var updatePermissions = ExtractPermissionsFromRecord(updateRestrictions);
                                        return new WithOrScopesCombiner(updatePermissions);
                                    }
                                    else if (method == "DELETE")
                                    {
                                        var deleteRestrictions = restrictedProperty.FindProperty("DeleteRestrictions")?.Value as IEdmRecordExpression;
                                        var deletePermissions = ExtractPermissionsFromRecord(deleteRestrictions);
                                        return new WithOrScopesCombiner(deletePermissions);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new DefaultScopesEvaluator();
        }
        
        private static IEnumerable<IEnumerable<ODataPathSegment>> ExtractExpandedNavigationProperties(SelectExpandClause selectExpandClause, IEnumerable<ODataPathSegment> root)
        {
            if (selectExpandClause != null)
            {
                foreach (var navigationSelectItem in selectExpandClause.SelectedItems.OfType<ExpandedNavigationSelectItem>())
                {
                    yield return root.Concat(navigationSelectItem.PathToNavigationProperty);

                    foreach (var childNavigationSelectItem in ExtractExpandedNavigationProperties(navigationSelectItem.SelectAndExpand, navigationSelectItem.PathToNavigationProperty))
                    {
                        yield return childNavigationSelectItem;
                    }
                }
            }
        }

        private static IScopesEvaluator GetNavigationPropertyPropertyOperationPermisions(IList<ODataPathSegment> pathSegments, bool isTargetByKey, IEdmModel model, string method)
        {
            if (pathSegments.Count <= 1)
            {
                return new DefaultScopesEvaluator();
            }

            var expectedPath = GetPathFromSegments(pathSegments);
            IEdmVocabularyAnnotatable root = (pathSegments[0] as EntitySetSegment).EntitySet as IEdmVocabularyAnnotatable ?? (pathSegments[0] as SingletonSegment).Singleton;

            var navRestrictions = root.VocabularyAnnotations(model).Where(a => a.Term.FullName() == ODataCapabilityRestrictionsConstants.NavigationRestrictions);
            foreach (var restriction in navRestrictions)
            {
                if (restriction.Value is IEdmRecordExpression record)
                {
                    var temp = record.FindProperty("RestrictedProperties");
                    if (temp?.Value is IEdmCollectionExpression restrictedProperties)
                    {
                        foreach (var item in restrictedProperties.Elements)
                        {
                            if (item is IEdmRecordExpression restrictedProperty)
                            {
                                var navigationProperty = restrictedProperty.FindProperty("NavigationProperty").Value as IEdmPathExpression;
                                if (navigationProperty?.Path == expectedPath)

                                {
                                    if (method == "GET")
                                    {
                                        var readRestrictions = restrictedProperty.FindProperty("ReadRestrictions")?.Value as IEdmRecordExpression;

                                        var readPermissions = ExtractPermissionsFromRecord(readRestrictions);
                                        var evaluator = new WithOrScopesCombiner(readPermissions);

                                        if (isTargetByKey)
                                        {
                                            var readByKeyRestrictions = readRestrictions.FindProperty("ReadByKeyRestrictions")?.Value as IEdmRecordExpression;
                                            var readByKeyPermissions = ExtractPermissionsFromRecord(readByKeyRestrictions);
                                            evaluator.AddRange(readByKeyPermissions);
                                        }

                                        return evaluator;
                                    }
                                    else if (method == "POST" || method == "PATCH" || method == "PUT" || method == "MERGE" || method == "DELETE")
                                    {
                                        var updateRestrictions = restrictedProperty.FindProperty("UpdateRestrictions")?.Value as IEdmRecordExpression;
                                        var updatePermissions = ExtractPermissionsFromRecord(updateRestrictions);
                                        return new WithOrScopesCombiner(updatePermissions);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return new DefaultScopesEvaluator();
        }

        private static IScopesEvaluator GetNavigationPropertyReadPermissions(IList<ODataPathSegment> pathSegments, bool isTargetByKey, IEdmModel model)
        {
            var expectedPath = GetPathFromSegments(pathSegments);
            IEdmVocabularyAnnotatable root = (pathSegments[0] as EntitySetSegment)?.EntitySet as IEdmVocabularyAnnotatable ?? (pathSegments[0] as SingletonSegment)?.Singleton ?? (pathSegments[0] as NavigationPropertySegment)?.NavigationSource as IEdmVocabularyAnnotatable;

            var navRestrictions = root.VocabularyAnnotations(model).Where(a => a.Term.FullName() == ODataCapabilityRestrictionsConstants.NavigationRestrictions);
            foreach (var restriction in navRestrictions)
            {
                if (restriction.Value is IEdmRecordExpression record)
                {
                    var temp = record.FindProperty("RestrictedProperties");
                    if (temp?.Value is IEdmCollectionExpression restrictedProperties)
                    {
                        foreach (var item in restrictedProperties.Elements)
                        {
                            if (item is IEdmRecordExpression restrictedProperty)
                            {
                                var navigationProperty = restrictedProperty.FindProperty("NavigationProperty").Value as IEdmPathExpression;
                                if (navigationProperty?.Path == expectedPath)
                                {
                                    var readRestrictions = restrictedProperty.FindProperty("ReadRestrictions")?.Value as IEdmRecordExpression;

                                    var readPermissions = ExtractPermissionsFromRecord(readRestrictions);
                                    var evaluator = new WithOrScopesCombiner(readPermissions);

                                    if (isTargetByKey)
                                    {
                                        var readByKeyRestrictions = readRestrictions.FindProperty("ReadByKeyRestrictions")?.Value as IEdmRecordExpression;
                                        var readByKeyPermissions = ExtractPermissionsFromRecord(readByKeyRestrictions);
                                        evaluator.AddRange(readByKeyPermissions);
                                    }

                                    return evaluator;
                                }
                            }
                        }
                    }
                }
            }

            return new DefaultScopesEvaluator();
        }

        static bool IsNextSegmentKey(ODataPath path, int currentPos)
        {
            return IsNextSegmentOfType<KeySegment>(path, currentPos);
        }

        static bool IsNextSegmentOfType<T>(ODataPath path, int currentPos)
        {
            var maxPos = path.Count - 1;

            if (maxPos <= currentPos)
            {
                return false;
            }

            var nextSegment = path.ElementAt(currentPos + 1);

            if (nextSegment is T)
            {
                return true;
            }

            if (nextSegment is TypeSegment && maxPos >= currentPos + 2 && path.ElementAt(currentPos + 2) is T)
            {
                return true;
            }

            return false;
        }

        static string GetPathFromSegments(IList<ODataPathSegment> segments)
        {
            var pathParts = new List<string>(segments.Count);
            var i = 0;
            foreach (var path in segments)
            {
                i++;

                if (path is EntitySetSegment entitySetSegment)
                {
                    pathParts.Add(entitySetSegment.EntitySet.Name);
                }
                else if(path is SingletonSegment singletonSegment)
                {
                    pathParts.Add(singletonSegment.Singleton.Name);
                }
                else if(path is NavigationPropertySegment navSegment)
                {
                    pathParts.Add(navSegment.NavigationProperty.Name);
                }
            }

            return string.Join('/', pathParts);
        }

        private static IScopesEvaluator GetSingletonPropertyOperationPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE" || method == "POST" || method == "DELETE")
            {
                return GetUpdatePermissions(annotations);
            }

            return new DefaultScopesEvaluator();
        }

        private static IScopesEvaluator GetEntityPropertyOperationPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadByKeyPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE" || method == "POST" || method == "DELETE")
            {
                return GetUpdatePermissions(annotations);
            }

            return new DefaultScopesEvaluator();
        }

        private static IScopesEvaluator GetNavigationSourceCrudPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);
            if (method == "GET")
            {
                return GetReadPermissions(annotations);
            }
            else if (method == "POST")
            {
                return GetInsertPermissions(annotations);
            }
            else if (method == "PATCH" || method == "PUT" || method == "MERGE")
            {
                return GetUpdatePermissions(annotations);
            }
            else if (method == "DELETE")
            {
                return GetDeletePermissions(annotations);
            }

            return new DefaultScopesEvaluator();
        }

        private static IScopesEvaluator GetEntityCrudPermissions(IEdmVocabularyAnnotatable target, IEdmModel model, string method)
        {
            var annotations = target.VocabularyAnnotations(model);

            if (method == "GET")
            {
                return GetReadByKeyPermissions(annotations);
            }
            else if (method == "PUT" || method == "POST" || method == "MERGE" || method == "PATCH")
            {
                return GetUpdatePermissions(annotations);
            }
            else if (method == "DELETE")
            {
                return GetDeletePermissions(annotations);
            }

            return new DefaultScopesEvaluator();
        }

        private static IScopesEvaluator GetReadPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var permissions = GetPermissions(ODataCapabilityRestrictionsConstants.ReadRestrictions, annotations);
            return new WithOrScopesCombiner(permissions);
        }

        private static IScopesEvaluator GetReadByKeyPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var evaluator = new WithOrScopesCombiner();
            foreach (var annotation in annotations)
            {
                if (annotation.Term.FullName() == ODataCapabilityRestrictionsConstants.ReadRestrictions && annotation.Value is IEdmRecordExpression record)
                {
                    var readPermissions = ExtractPermissionsFromAnnotation(annotation);
                    evaluator.AddRange(readPermissions);

                    var readByKeyProperty = record.FindProperty("ReadByKeyRestrictions");
                    var readByKeyValue = readByKeyProperty?.Value as IEdmRecordExpression;
                    var permissionsProperty = readByKeyValue?.FindProperty("Permissions");
                    var  readByKeyPermissions = ExtractPermissionsFromProperty(permissionsProperty);
                    evaluator.AddRange(readByKeyPermissions);
                }
            }

            return evaluator;
        }

        private static IScopesEvaluator GetInsertPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var permissions = GetPermissions(ODataCapabilityRestrictionsConstants.InsertRestrictions, annotations);
            return new WithOrScopesCombiner(permissions);
        }

        private static IScopesEvaluator GetDeletePermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var permissions = GetPermissions(ODataCapabilityRestrictionsConstants.DeleteRestrictions, annotations);
            return new WithOrScopesCombiner(permissions);
        }

        private static IScopesEvaluator GetUpdatePermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var permissions = GetPermissions(ODataCapabilityRestrictionsConstants.UpdateRestrictions, annotations);
            return new WithOrScopesCombiner(permissions);
        }

        private static IScopesEvaluator GetOperationPermissions(IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            var permissions = GetPermissions(ODataCapabilityRestrictionsConstants.OperationRestrictions, annotations);
            return new WithOrScopesCombiner(permissions);
        }

        private static IEnumerable<IScopesEvaluator> GetPermissions(string restrictionType, IEnumerable<IEdmVocabularyAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Term.FullName() == restrictionType)
                {
                    return ExtractPermissionsFromAnnotation(annotation);
                }
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static IEnumerable<IScopesEvaluator> ExtractPermissionsFromAnnotation(IEdmVocabularyAnnotation annotation)
        {
            return ExtractPermissionsFromRecord(annotation.Value as IEdmRecordExpression);
        }

        private static IEnumerable<IScopesEvaluator> ExtractPermissionsFromRecord(IEdmRecordExpression record)
        {
            var permissionsProperty = record?.FindProperty("Permissions");
            return ExtractPermissionsFromProperty(permissionsProperty);
        }

        private static IEnumerable<IScopesEvaluator> ExtractPermissionsFromProperty(IEdmPropertyConstructor permissionsProperty)
        {
            if (permissionsProperty?.Value is IEdmCollectionExpression permissionsValue)
            {
                return permissionsValue.Elements.OfType<IEdmRecordExpression>().Select(p => GetPermissionData(p));
            }

            return Enumerable.Empty<PermissionData>();
        }

        private static PermissionData GetPermissionData(IEdmRecordExpression permissionRecord)
        {
            var schemeProperty = permissionRecord.FindProperty("SchemeName")?.Value as IEdmStringConstantExpression;
            var scopesProperty = permissionRecord.FindProperty("Scopes")?.Value as IEdmCollectionExpression;

            var scopes = scopesProperty.Elements.Select(s => GetScopeData(s as IEdmRecordExpression));

            return new PermissionData() { SchemeName = schemeProperty.Value, Scopes = scopes.ToList() };
        }

        private static PermissionScopeData GetScopeData(IEdmRecordExpression scopeRecord)
        {
            var scopeProperty = scopeRecord.FindProperty("Scope")?.Value as IEdmStringConstantExpression;
            var restrictedPropertiesProperty = scopeRecord.FindProperty("RestrictedProperties")?.Value as IEdmStringConstantExpression;

            return new PermissionScopeData() { Scope = scopeProperty?.Value, RestrictedProperties = restrictedPropertiesProperty?.Value };
        }
    }
}
