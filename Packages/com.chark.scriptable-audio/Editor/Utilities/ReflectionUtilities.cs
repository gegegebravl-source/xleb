using System;
using System.Collections;
using System.Reflection;
using UnityEditor;

namespace CHARK.ScriptableAudio.Editor.Utilities
{
    // For more info see:
    // https://github.com/lordofduct/spacepuppy-unity-framework-4.0/blob/master/Framework/com.spacepuppy.core/Editor/src/EditorHelper.cs
    internal static class ReflectionUtilities
    {
        // TODO: new Unity versions can just use boxedValue
        internal static object GetTargetObjectOfProperty(this SerializedProperty property)
        {
            if (property == default)
            {
                return default;
            }

            object targetObject = property.serializedObject.targetObject;

            var propertyPath = property.propertyPath.Replace(".Array.data[", "[");
            var propertyParts = propertyPath.Split('.');
            foreach (var propertyPart in propertyParts)
            {
                if (propertyPart.Contains("["))
                {
                    var partNameIndex = propertyPart.IndexOf("[", StringComparison.Ordinal);
                    var partName = propertyPart.Substring(0, partNameIndex);

                    var partValue = propertyPart
                        .Substring(partNameIndex)
                        .Replace("[", "")
                        .Replace("]", "");

                    var partIndex = Convert.ToInt32(partValue);

                    targetObject = GetValue(targetObject, partName, partIndex);
                }
                else
                {
                    targetObject = GetValue(targetObject, propertyPart);
                }
            }

            return targetObject;
        }

        private static object GetValue(object source, string name, int index)
        {
            if (!(GetValue(source, name) is IEnumerable enumerable))
            {
                return default;
            }

            var enumerator = enumerable.GetEnumerator();
            for (var enumeratorIndex = 0; enumeratorIndex <= index; enumeratorIndex++)
            {
                if (enumerator.MoveNext())
                {
                    continue;
                }

                return default;
            }

            return enumerator.Current;
        }

        private static object GetValue(object source, string name)
        {
            if (source == default)
            {
                return default;
            }

            var type = source.GetType();

            while (type != default)
            {
                var field = type.GetField(
                    name,
                    BindingFlags.NonPublic
                    | BindingFlags.Public
                    | BindingFlags.Instance
                );

                if (field != default)
                {
                    return field.GetValue(source);
                }

                var prop = type.GetProperty(
                    name,
                    BindingFlags.NonPublic
                    | BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.IgnoreCase
                );

                if (prop != default)
                {
                    return prop.GetValue(source, default);
                }

                type = type.BaseType;
            }

            return default;
        }
    }
}
