// Author: František Holubec
// Created: 19.03.2026

using System.Collections;
using System.Collections.Generic;

namespace EDIVE.OdinExtensions.Editor
{
    public sealed class TypeEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
    {
        public static readonly TypeEqualityComparer<T> INSTANCE = new();
        
        private TypeEqualityComparer() { }
        
        public bool Equals(T x, T y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.GetType() == y.GetType();
        }

        public int GetHashCode(T obj)
        {
            return obj?.GetType().GetHashCode() ?? 0;
        }
        
        bool IEqualityComparer.Equals(object x, object y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.GetType() == y.GetType();
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj?.GetType().GetHashCode() ?? 0;
        }
    }
}
