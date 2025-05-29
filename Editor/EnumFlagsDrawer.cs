#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace UnityEssentials
{
    /// <summary>
    /// A custom property drawer for rendering enums with the <see cref="EnumFlagsAttribute"/> in the Unity Editor.
    /// </summary>
    /// <remarks>This drawer provides a foldout interface for selecting multiple enum values using checkboxes.
    /// It also includes "All" and "None" buttons for quickly selecting or deselecting all options. The drawer is
    /// designed to work with enums that have the <see cref="FlagsAttribute"/> applied.</remarks>
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    public class EnumFlagsDrawer : PropertyDrawer
    {
        private bool _foldoutOpen = false;

        private object _theEnum;
        private Array _enumValues;
        private Type _enumUnderlyingType;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * (_foldoutOpen ? Enum.GetValues(fieldInfo.FieldType).Length + 2 : 1);

        /// <summary>
        /// Renders a custom GUI for an enum property with support for flag-based selection.
        /// </summary>
        /// <remarks>This method provides a custom inspector interface for enum fields marked with a
        /// specific attribute, allowing users to toggle individual flags or select "All" or "None" options. If the
        /// property is not an enum, an error message is displayed instead.</remarks>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.HelpBox(position, "EnumFlags attribute only supports enum fields.", MessageType.Error);
                return;
            }

            _theEnum = fieldInfo.GetValue(property.serializedObject.targetObject);
            _enumValues = Enum.GetValues(_theEnum.GetType());
            _enumUnderlyingType = Enum.GetUnderlyingType(_theEnum.GetType());

            //We need to convert the enum to its underlying type, if we don't it will be boxed
            //into an object later and then we would need to unbox it like (UnderlyingType)(EnumType)theEnum.
            //If we do this here we can just do (UnderlyingType)theEnum later (plus we can visualize the value of theEnum in VS when debugging)
            _theEnum = Convert.ChangeType(_theEnum, _enumUnderlyingType);

            EditorGUI.BeginProperty(position, label, property);

            var foldoutPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            _foldoutOpen = EditorGUI.Foldout(foldoutPosition, _foldoutOpen, label, true);

            if (_foldoutOpen)
            {
                EditorGUI.indentLevel++;
                {
                    //Draw the All button
                    if (GUI.Button(new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 1, 30, 15), "All"))
                        _theEnum = DoNotOperator(Convert.ChangeType(0, _enumUnderlyingType), _enumUnderlyingType);

                    //Draw the None button
                    if (GUI.Button(new Rect(position.x + 32, position.y + EditorGUIUtility.singleLineHeight * 1, 40, 15), "None"))
                        _theEnum = Convert.ChangeType(0, _enumUnderlyingType);

                    //Draw the list
                    for (int i = 0; i < Enum.GetNames(fieldInfo.FieldType).Length; i++)
                        if (EditorGUI.Toggle(new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * (2 + i), position.width, EditorGUIUtility.singleLineHeight), Enum.GetNames(fieldInfo.FieldType)[i], IsSet(i)))
                            ToggleIndex(i, true);
                        else ToggleIndex(i, false);
                }
                EditorGUI.indentLevel--;
            }

            fieldInfo.SetValue(property.serializedObject.targetObject, _theEnum);
            property.serializedObject.ApplyModifiedProperties();
        }

        private object GetEnumValue(int index) =>
            Convert.ChangeType(_enumValues.GetValue(index), _enumUnderlyingType);

        /// <summary>
        /// Toggles the inclusion of a specific enum value in the current state.
        /// </summary>
        /// <remarks>If <paramref name="set"/> is <see langword="true"/> and the specified index
        /// corresponds to a "None" element, the current state is reset to the default value. If <paramref name="set"/>
        /// is <see langword="false"/> and the specified index corresponds to a "None" or "All" element, the operation
        /// is ignored.</remarks>
        /// <param name="index">The index of the enum value to toggle. Must correspond to a valid enum value.</param>
        /// <param name="set">A boolean value indicating whether to include or exclude the enum value. <see langword="true"/> to include
        /// the value; <see langword="false"/> to exclude it.</param>
        private void ToggleIndex(int index, bool set)
        {
            if (set)
            {
                if (IsNoneElement(index))
                    _theEnum = Convert.ChangeType(0, _enumUnderlyingType);

                //enum = enum | value
                _theEnum = DoOrOperator(_theEnum, GetEnumValue(index), _enumUnderlyingType);
            }
            else
            {
                if (IsNoneElement(index) || IsAllElement(index))
                    return;

                object value = GetEnumValue(index);
                object notValue = DoNotOperator(value, _enumUnderlyingType);

                //enum = enum & ~value
                _theEnum = DoAndOperator(_theEnum, notValue, _enumUnderlyingType);
            }

        }

        /// <summary>
        /// Determines whether the specified enum element, identified by its index, is set.
        /// </summary>
        /// <remarks>This method handles special cases for "All" and "None" elements in the enum: <list
        /// type="bullet"> <item> For "All" elements, it checks whether all other visible elements are set, accounting
        /// for any missing bits. </item> <item> For "None" elements, it checks whether the enum's value is zero.
        /// </item> </list> For all other elements, the method performs a bitwise operation to determine if the element
        /// is set.</remarks>
        /// <param name="index">The zero-based index of the enum element to check.</param>
        /// <returns><see langword="true"/> if the specified enum element is set; otherwise, <see langword="false"/>.</returns>
        private bool IsSet(int index)
        {
            object value = DoAndOperator(_theEnum, GetEnumValue(index), _enumUnderlyingType);

            //We handle All and None elements differently, since they're "special"
            if (IsAllElement(index))
            {
                //If all other bits visible to the user (elements) are set, the "All" element checkbox has to be checked
                //We don't do a simple AND operation because there might be missing bits.
                //e.g. An enum with 6 elements including the "All" element. If we set all bits visible except the "All" bit,
                //two bits might be unset. Since we want the "All" element checkbox to be checked when all other elements are set
                //we have to make sure those two extra bits are also set.
                bool allSet = true;
                for (int i = 0; i < Enum.GetNames(fieldInfo.FieldType).Length; i++)
                    if (i != index && !IsNoneElement(i) && !IsSet(i))
                    {
                        allSet = false;
                        break;
                    }

                //Make sure all bits are set if all "visible bits" are set
                if (allSet)
                    _theEnum = DoNotOperator(Convert.ChangeType(0, _enumUnderlyingType), _enumUnderlyingType);

                return allSet;
            }
            else if (IsNoneElement(index))
                //Just check the "None" element checkbox our enum's value is 0
                return Convert.ChangeType(_theEnum, _enumUnderlyingType).Equals(Convert.ChangeType(0, _enumUnderlyingType));

            return !value.Equals(Convert.ChangeType(0, _enumUnderlyingType));
        }

        private static object DoOrOperator(object a, object b, Type type)
        {
            return type switch
            {
                Type t when t == typeof(int) => (int)a | (int)b,
                Type t when t == typeof(uint) => (uint)a | (uint)b,
                Type t when t == typeof(short) => unchecked((short)((short)a | (short)b)),
                Type t when t == typeof(ushort) => unchecked((ushort)((ushort)a | (ushort)b)),
                Type t when t == typeof(long) => (long)a | (long)b,
                Type t when t == typeof(ulong) => (ulong)a | (ulong)b,
                Type t when t == typeof(byte) => unchecked((byte)((byte)a | (byte)b)),
                Type t when t == typeof(sbyte) => unchecked((sbyte)((sbyte)a | (sbyte)b)),
                _ => throw new ArgumentException($"Type {type.FullName} not supported.")
            };
        }

        private static object DoAndOperator(object a, object b, Type type)
        {
            return type switch
            {
                Type t when t == typeof(int) => (int)a & (int)b,
                Type t when t == typeof(uint) => (uint)a & (uint)b,
                Type t when t == typeof(short) => unchecked((short)((short)a & (short)b)),
                Type t when t == typeof(ushort) => unchecked((ushort)((ushort)a & (ushort)b)),
                Type t when t == typeof(long) => (long)a & (long)b,
                Type t when t == typeof(ulong) => (ulong)a & (ulong)b,
                Type t when t == typeof(byte) => unchecked((byte)((byte)a & (byte)b)),
                Type t when t == typeof(sbyte) => unchecked((sbyte)((sbyte)a & (sbyte)b)),
                _ => throw new ArgumentException($"Type {type.FullName} not supported.")
            };
        }

        private static object DoNotOperator(object a, Type type)
        {
            return type switch
            {
                Type t when t == typeof(int) => ~(int)a,
                Type t when t == typeof(uint) => ~(uint)a,
                Type t when t == typeof(short) => unchecked((short)~(short)a),
                Type t when t == typeof(ushort) => unchecked((ushort)~(ushort)a),
                Type t when t == typeof(long) => ~(long)a,
                Type t when t == typeof(ulong) => ~(ulong)a,
                Type t when t == typeof(byte) => unchecked((byte)~(byte)a),
                Type t when t == typeof(sbyte) => unchecked((sbyte)~(sbyte)a),
                _ => throw new ArgumentException($"Type {type.FullName} not supported.")
            };
        }

        private bool IsNoneElement(int index) =>
            GetEnumValue(index).Equals(Convert.ChangeType(0, _enumUnderlyingType));

        private bool IsAllElement(int index)
        {
            object elemValue = GetEnumValue(index);
            return elemValue.Equals(DoNotOperator(Convert.ChangeType(0, _enumUnderlyingType), _enumUnderlyingType));
        }
    }
}
#endif