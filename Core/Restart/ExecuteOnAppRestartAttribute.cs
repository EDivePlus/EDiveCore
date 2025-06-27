// Author: František Holubec
// Created: 27.06.2025

using System;
using System.Reflection;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace EDIVE.Core.Restart
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class ExecuteOnAppRestartAttribute : AAppRestartAttribute
    {
        public override string ActionName => "Execute";
        
        public ExecuteOnAppRestartAttribute(int order) : base(order) { }
        
        public override async UniTask Process(MemberInfo member)
        {
            if (member is MethodInfo method)
            {
                var result = method.Invoke(null, null);
                if (result is UniTask uniTask)
                    await uniTask;
            }
        }
    }
}
