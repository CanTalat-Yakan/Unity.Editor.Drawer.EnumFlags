using System;
using UnityEngine;

namespace UnityEssentials
{
    /// <summary>
    /// Indicates that an enumeration field should be treated as a set of bitwise flags.
    /// </summary>
    /// <remarks>This attribute is typically applied to enumeration fields to signify that the values  can be
    /// combined using bitwise operations. It is commonly used in conjunction with  enums marked with the <see
    /// cref="FlagsAttribute"/>.</remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumFlagsAttribute : PropertyAttribute { }
}
