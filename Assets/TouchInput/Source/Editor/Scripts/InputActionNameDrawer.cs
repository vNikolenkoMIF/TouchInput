using System;
using System.Collections.Generic;
using TouchInput.Source.Actions.Scripts.Contracts;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TouchInput.Source.Editor.Scripts
{
    [CustomPropertyDrawer(typeof(InputActionName))]
    public sealed class InputActionNameDrawer : PropertyDrawer
    {
        private const string NONE_LABEL = "(None)";

        private static GUIContent[] _cachedDisplayOptions;
        private static string[] _cachedStoredValues;
        private static bool _cacheValid;

        static InputActionNameDrawer()
        {
            EditorApplication.projectChanged += InvalidateCache;
        }

        private static void InvalidateCache() => _cacheValid = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("_value");
            var currentValue = valueProp.stringValue;

            var (displayOptions, storedValues) = CollectActions(currentValue);

            var currentIndex = Array.IndexOf(storedValues, currentValue);
            if (currentIndex < 0) currentIndex = 0;

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var newIndex = EditorGUI.Popup(position, label, currentIndex, displayOptions);

            if (EditorGUI.EndChangeCheck())
                valueProp.stringValue = newIndex == 0 ? string.Empty : storedValues[newIndex];

            EditorGUI.EndProperty();
        }

        private static (GUIContent[] displayOptions, string[] storedValues) CollectActions(string currentValue)
        {
            if (!_cacheValid)
                RebuildCache();

            if (string.IsNullOrEmpty(currentValue) || Array.IndexOf(_cachedStoredValues, currentValue) >= 0)
                return (_cachedDisplayOptions, _cachedStoredValues);

            // Current value is missing from all assets — append a [Missing] entry without polluting the cache.
            var displayOptions = new GUIContent[_cachedDisplayOptions.Length + 1];
            var storedValues = new string[_cachedStoredValues.Length + 1];
            _cachedDisplayOptions.CopyTo(displayOptions, 0);
            _cachedStoredValues.CopyTo(storedValues, 0);
            displayOptions[displayOptions.Length - 1] = new GUIContent($"[Missing] {currentValue}");
            storedValues[storedValues.Length - 1] = currentValue;
            return (displayOptions, storedValues);
        }

        private static void RebuildCache()
        {
            var displayList = new List<GUIContent> { new GUIContent(NONE_LABEL) };
            var valueList = new List<string> { string.Empty };

            var guids = AssetDatabase.FindAssets("t:InputActionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
                if (asset == null)
                    continue;

                foreach (var map in asset.actionMaps)
                {
                    foreach (var action in map.actions)
                    {
                        displayList.Add(new GUIContent($"{map.name}/{action.name}"));
                        valueList.Add(action.name);
                    }
                }
            }

            _cachedDisplayOptions = displayList.ToArray();
            _cachedStoredValues = valueList.ToArray();
            _cacheValid = true;
        }
    }
}
