using System;
using UnityEngine.WSA;

namespace PolymorphicInspector
{
    /// <summary>
    /// Overrides the name or folder of this type listed in Polymorphic Inspectors.
    /// This only works with serialized non-monobehavior objects.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class PolymorphicOverrideAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Folder;
        public readonly bool IsFolderSet;
        public PolymorphicOverrideAttribute(string name, string folder)
        {
            Name = name;
            Folder = folder;
            IsFolderSet = true;
        }
        public PolymorphicOverrideAttribute(string name)
        {
            Name = name;
            Folder = "";
            IsFolderSet = false;
        }
    }
}
