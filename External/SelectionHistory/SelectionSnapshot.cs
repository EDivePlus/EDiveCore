using System;
using Sirenix.OdinInspector;
using Object = UnityEngine.Object;

namespace EDIVE.External.SelectionHistory
{
    [Serializable]
    public class SelectionSnapshot
    {
        [LabelText("Object")]
        [LabelWidth(60)]
        public Object ActiveObject;
        
        [ShowIf(nameof(IsMultiple))]
        public Object[] Objects;
        
        [ShowIf(nameof(Context))]
        [LabelWidth(60)]
        public Object Context;

        public bool IsEmpty => ActiveObject == null;
        public bool IsMultiple => Objects != null && Objects.Length > 1;

        public SelectionSnapshot(Object activeObject, Object[] objects, Object context = null)
        {
            ActiveObject = activeObject;
            Objects = objects;
            Context = context;
        }

        public override string ToString()
        {
            return $"Active: {ActiveObject} Objects: {Objects.Length} Context: {Context}";
        }
    }
}