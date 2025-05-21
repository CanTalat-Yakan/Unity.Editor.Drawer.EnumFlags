using System;
using UnityEngine;

namespace UnityEssentials
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class EnumFlagsAttribute : PropertyAttribute { }
}
