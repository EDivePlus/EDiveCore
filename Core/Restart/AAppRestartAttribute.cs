// Author: František Holubec
// Created: 27.06.2025

using System;
using System.Reflection;
using Cysharp.Threading.Tasks;

namespace EDIVE.Core.Restart
{
    public abstract class AAppRestartAttribute : Attribute
    {
        public int Order { get; }
        public abstract string ActionName { get; }

        protected AAppRestartAttribute(int order) => Order = order;

        public abstract UniTask Process(MemberInfo member);
    }
}
