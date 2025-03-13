// Source: https://forum.unity.com/threads/attribute-to-clear-static-fields-on-play-start.790226/
// https://github.com/joshcamas/unity-domain-reload-helper/tree/master

namespace EDIVE.External.DomainReloadHelper
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public class ClearOnReloadAttribute : DomainReloadHelperAttribute
    {
        public readonly object ValueToAssign;
        public readonly bool AssignNewTypeInstance;

        /// <summary>
        ///     Marks field, property or event to be cleared on reload.
        /// </summary>
        public ClearOnReloadAttribute() : base(0)
        {
            ValueToAssign = null;
            AssignNewTypeInstance = false;
        }

        /// <summary>
        ///     Marks field of property to be cleared and assigned given value on reload.
        /// </summary>
        /// <param name="valueToAssign">Explicit value which will be assigned to field/property on reload. Has to match field/property type. Has no effect on events.</param>
        public ClearOnReloadAttribute(object valueToAssign) : base(0)
        {
            ValueToAssign = valueToAssign;
            AssignNewTypeInstance = false;
        }

        /// <summary>
        ///     Marks field of property to be cleared or re-initialized on reload.
        /// </summary>
        /// <param name="assignNewTypeInstance">If true, field/property will be assigned a newly created object of its type on reload. Has no effect on events.</param>
        public ClearOnReloadAttribute(bool assignNewTypeInstance = false) : base(0)
        {
            ValueToAssign = null;
            AssignNewTypeInstance = assignNewTypeInstance;
        }

        /// <summary>
        ///     Marks field of property to be cleared and assigned given value on reload.
        /// </summary>
        /// <param name="order">Execution order</param>
        public ClearOnReloadAttribute(int order) : base(order)
        {
            ValueToAssign = null;
            AssignNewTypeInstance = false;
        }
    }
}
