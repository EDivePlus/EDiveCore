// Author: Michal Petr
// Created: 09.03.2026

#if ZLINQ
using System;
using ZLinq;

namespace EDIVE.NativeUtils
{
    public static class ZLinqExtensions
    {
        public static void ForEach<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> source, Action<T> action) where TEnumerator : struct, IValueEnumerator<T>
        {
            foreach (var item in source)
                action(item);
        }

        public static bool TryGetFirst<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> source, out T result) where TEnumerator : struct, IValueEnumerator<T>
        {
            foreach (var element in source)
            {
                result = element;
                return true;
            }

            result = default;
            return false;
        }
        
        public static bool TryGetFirst<TEnumerator, T>(this ValueEnumerable<TEnumerator, T> source, Predicate<T> predicate, out T result) where TEnumerator : struct, IValueEnumerator<T>
        {
            if (predicate == null)
            {
                result = default;
                return false;
            }

            foreach (var element in source)
            {
                if (!predicate(element))
                    continue;

                result = element;
                return true;
            }

            result = default;
            return false;
        }
    }
}
#endif