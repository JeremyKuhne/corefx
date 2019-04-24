﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal sealed partial class JsonClassInfo
    {
        private readonly List<PropertyRef> _propertyRefs = new List<PropertyRef>();

        // Cache of properties by first ordering. Use an array for highest performance.
        private volatile PropertyRef[] _propertyRefsSorted = null;

        internal delegate object ConstructorDelegate();
        internal ConstructorDelegate CreateObject { get; private set; }

        internal ClassType ClassType { get; private set; }

        // If enumerable, the JsonClassInfo for the element type.
        internal JsonClassInfo ElementClassInfo { get; private set; }

        public Type Type { get; private set; }

        internal void UpdateSortedPropertyCache(ref ReadStackFrame frame)
        {
            // Todo: on classes with many properties (about 50) we need to switch to a hashtable for performance.
            // Todo: when using PropertyNameCaseInsensitive we also need to use the hashtable with case-insensitive
            // comparison to handle Turkish etc. cultures properly.

            // Set the sorted property cache. Overwrite any existing cache which can occur in multi-threaded cases.
            if (frame.PropertyRefCache != null)
            {
                List<PropertyRef> cache = frame.PropertyRefCache;

                // Add any missing properties. This creates a consistent cache count which is important for
                // the loop in GetProperty() when there are multiple threads in a race conditions each generating
                // a cache for a given POCO type but with different property counts in the json.
                while (cache.Count < _propertyRefs.Count)
                {
                    for (int iProperty = 0; iProperty < _propertyRefs.Count; iProperty++)
                    {
                        PropertyRef propertyRef = _propertyRefs[iProperty];

                        bool found = false;
                        int iCacheProperty = 0;
                        for (; iCacheProperty < cache.Count; iCacheProperty++)
                        {
                            if (propertyRef.Equals(cache[iCacheProperty]))
                            {
                                // The property is already cached, skip to the next property.
                                found = true;
                                break;
                            }
                        }

                        if (found == false)
                        {
                            cache.Add(propertyRef);
                            break;
                        }
                    }
                }

                Debug.Assert(cache.Count == _propertyRefs.Count);
                _propertyRefsSorted = cache.ToArray();
                frame.PropertyRefCache = null;
            }
        }

        internal JsonClassInfo(Type type, JsonSerializerOptions options)
        {
            Type = type;
            ClassType = GetClassType(type);

            CreateObject = options.ClassMaterializerStrategy.CreateConstructor(type);

            // Ignore properties on enumerable.
            if (ClassType == ClassType.Object)
            {
                var propertyNames = new HashSet<string>(StringComparer.Ordinal);

                foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // For now we only support public getters\setters
                    if (propertyInfo.GetMethod?.IsPublic == true ||
                        propertyInfo.SetMethod?.IsPublic == true)
                    {
                        JsonPropertyInfo jsonPropertyInfo = AddProperty(propertyInfo.PropertyType, propertyInfo, type, options);

                        if (jsonPropertyInfo.NameAsString == null)
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(this, jsonPropertyInfo);
                        }

                        // If the JsonPropertyNameAttribute or naming policy results in collisions, throw an exception.
                        if (!propertyNames.Add(jsonPropertyInfo.NameUsedToCompareAsString))
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(this, jsonPropertyInfo);
                        }

                        jsonPropertyInfo.ClearUnusedValuesAfterAdd();
                    }
                }
            }
            else if (ClassType == ClassType.Enumerable || ClassType == ClassType.Dictionary)
            {
                // Add a single property that maps to the class type so we can have policies applied.
                JsonPropertyInfo jsonPropertyInfo = AddProperty(type, propertyInfo: null, type, options);

                // Use the type from the property policy to get any late-bound concrete types (from an interface like IDictionary).
                CreateObject = options.ClassMaterializerStrategy.CreateConstructor(jsonPropertyInfo.RuntimePropertyType);

                // Create a ClassInfo that maps to the element type which is used for (de)serialization and policies.
                Type elementType = GetElementType(type);

                ElementClassInfo = options.GetOrAddClass(elementType);
            }
            else if (ClassType == ClassType.Value)
            {
                // Add a single property that maps to the class type so we can have policies applied.
                AddProperty(type, propertyInfo: null, type, options);
            }
            else
            {
                Debug.Assert(ClassType == ClassType.Unknown);
                // Do nothing. The type is typeof(object).
            }
        }

        internal JsonPropertyInfo GetProperty(JsonSerializerOptions options, ReadOnlySpan<byte> propertyName, ref ReadStackFrame frame)
        {
            // If we should compare with case-insensitive, normalize to an uppercase format since that is what is cached on the propertyInfo.
            if (options.PropertyNameCaseInsensitive)
            {
                string utf16PropertyName = Encoding.UTF8.GetString(propertyName.ToArray());
                string upper = utf16PropertyName.ToUpperInvariant();
                propertyName = Encoding.UTF8.GetBytes(upper);
            }

            ulong key = PropertyRef.GetKey(propertyName);
            JsonPropertyInfo propertyInfo = null;

            // First try sorted lookup.
            int propertyIndex = frame.PropertyIndex;

            // If we're not trying to build the cache locally, and there is an existing cache, then use it.
            bool hasPropertyCache = frame.PropertyRefCache == null && _propertyRefsSorted != null;
            if (hasPropertyCache)
            {
                // This .Length is consistent no matter what json data intialized _propertyRefsSorted.
                int count = _propertyRefsSorted.Length;
                if (count != 0)
                {
                    int iForward = propertyIndex;
                    int iBackward = propertyIndex - 1;
                    while (iForward < count || iBackward >= 0)
                    {
                        if (iForward < count)
                        {
                            ref PropertyRef cachedProperty = ref _propertyRefsSorted[iForward];
                            if (cachedProperty.Equals(propertyName, key))
                            {
                                return cachedProperty.Info;
                            }
                            ++iForward;
                        }

                        if (iBackward >= 0)
                        {
                            ref PropertyRef cachedProperty = ref _propertyRefsSorted[iBackward];
                            if (cachedProperty.Equals(propertyName, key))
                            {
                                return cachedProperty.Info;
                            }
                            --iBackward;
                        }
                    }
                }
            }

            // Try the main list which has all of the properties in a consistent order.
            // We could get here even when hasPropertyCache==true if there is a race condition with different json
            // property ordering and _propertyRefsSorted is re-assigned while in the loop above.
            for (int i = 0; i < _propertyRefs.Count; i++)
            {
                PropertyRef property = _propertyRefs[i];
                if (property.Equals(propertyName, key))
                {
                    propertyInfo = property.Info;
                    break;
                }
            }

            if (!hasPropertyCache)
            {
                if (propertyIndex == 0)
                {
                    // Create the temporary list on first property access to prevent a partially filled List.
                    Debug.Assert(frame.PropertyRefCache == null);
                    frame.PropertyRefCache = new List<PropertyRef>();
                }

                if (propertyInfo != null)
                {
                    Debug.Assert(frame.PropertyRefCache != null);
                    frame.PropertyRefCache.Add(new PropertyRef(key, propertyInfo));
                }
            }

            return propertyInfo;
        }

        internal JsonPropertyInfo GetPolicyProperty()
        {
            Debug.Assert(_propertyRefs.Count == 1);
            return _propertyRefs[0].Info;
        }

        internal JsonPropertyInfo GetProperty(int index)
        {
            Debug.Assert(index < _propertyRefs.Count);
            return _propertyRefs[index].Info;
        }

        internal int PropertyCount
        {
            get
            {
                return _propertyRefs.Count;
            }
        }

        public static Type GetElementType(Type propertyType)
        {
            Type elementType = null;
            if (typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                elementType = propertyType.GetElementType();
                if (elementType == null)
                {
                    Type[] args = propertyType.GetGenericArguments();

                    if (propertyType.IsGenericType)
                    {
                        if (GetClassType(propertyType) == ClassType.Dictionary)
                        {
                            elementType = args[1];
                        }
                        else
                        {
                            elementType = args[0];
                        }
                    }
                    else
                    {
                        // Unable to determine collection type; attempt to use object which will be used to create loosely-typed collection.
                        return typeof(object);
                    }
                }
            }

            return elementType;
        }

        internal static ClassType GetClassType(Type type)
        {
            Debug.Assert(type != null);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            // A Type is considered a value if it implements IConvertible or is a DateTimeOffset or is a JsonElement.
            if (typeof(IConvertible).IsAssignableFrom(type) || type == typeof(DateTimeOffset) || type == typeof(JsonElement))
            {
                return ClassType.Value;
            }

            if (typeof(IDictionary).IsAssignableFrom(type) || 
                (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) 
                || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))))
            {
                return ClassType.Dictionary;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return ClassType.Enumerable;
            }

            if (type == typeof(object))
            {
                return ClassType.Unknown;
            }

            return ClassType.Object;
        }
    }
}
