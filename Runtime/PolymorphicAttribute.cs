namespace PolymorphicInspector
{
    /// <summary>
    /// Creates a dropdown in the Inspector allowing Unity to populate the property this attribute belongs to with an instance of a derived class.
    /// This allows cool polymorphism functionality, similiar to how UObjects work in Unreal.
    /// This only works with serialized non-monobehavior objects.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public class PolymorphicAttribute : UnityEngine.PropertyAttribute
    {
        public PolymorphicAttribute() : base() { }
    }
}
