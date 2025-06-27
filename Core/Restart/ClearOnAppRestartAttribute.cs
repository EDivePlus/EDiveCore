// Author: František Holubec
// Created: 27.06.2025

using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace EDIVE.Core.Restart
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ClearOnAppRestartAttribute : AAppRestartAttribute
    {
        public override string ActionName => "Clear";
        
        public ClearOnAppRestartAttribute(int order) : base(order) { }
        
        public override UniTask Process(MemberInfo member)
        {
            if (member is FieldInfo field)
                field.SetValue(null, null);
            
            if (member is PropertyInfo property)
                property.SetValue(null, null);
            
            return UniTask.CompletedTask;
        }
    }
}
