// Author: Michal Petr
// Created: 19.10.2026

using System.Collections;
using System.Runtime.CompilerServices;

namespace EDIVE.OdinExtensions.Editor
{
    public sealed class ReferenceEqualityComparer : IEqualityComparer
    {
        public static readonly ReferenceEqualityComparer INSTANCE = new();

        bool IEqualityComparer.Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }
    }
}
