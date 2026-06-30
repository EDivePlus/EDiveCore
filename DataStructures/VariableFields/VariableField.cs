// Author: František Holubec
// Created: 20.10.2025

using System;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.DataStructures.VariableFields
{
    [Serializable]
    public class VariableField<T>
    {
        [SerializeReference]
        private IVariableFieldData<T> _Data = new RawVariableFieldData<T>();
        
        public IVariableFieldData<T> Data => _Data ??= new RawVariableFieldData<T>();
        
        public T Value
        {
            get => Data.Value;
            set => Data.Value = value;
        }

        public VariableField() { }
        public VariableField(T value)
        {
            _Data = new RawVariableFieldData<T> { Value = value };
        }
        public VariableField(IVariableFieldData<T> data)
        {
            _Data = data;
        }
    }

#if UNITY_EDITOR
    public sealed class VariableFieldDrawer<T> : OdinValueDrawer<VariableField<T>>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            Property.Children["_Data"].Draw(label);
        }
    }
#endif
}
