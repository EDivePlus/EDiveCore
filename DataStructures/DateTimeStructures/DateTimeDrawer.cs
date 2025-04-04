#if UNITY_EDITOR
using JetBrains.Annotations;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.DataStructures.DateTimeStructures
{
    [UsedImplicitly]
    public sealed class UDateTimeDrawer : OdinValueDrawer<UDateTime>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            DateTimeDrawerUtility.DrawDateTimeField(label, ValueEntry.SmartValue, value =>
            {
                ValueEntry.SmartValue = value;
                ValueEntry.ApplyChanges();
                Property.MarkSerializationRootDirty();
            });
        }
    }

    [UsedImplicitly]
    public sealed class DateTimeDrawer : OdinValueDrawer<System.DateTime>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            DateTimeDrawerUtility.DrawDateTimeField(label, ValueEntry.SmartValue, value =>
            {
                ValueEntry.WeakValues.ForceSetValue(0, value); // Because DateTime is fucking bullshit and does not compare Kind
                ValueEntry.ApplyChanges();
                Property.MarkSerializationRootDirty();
            });
        }
    }
}
#endif
