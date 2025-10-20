// Author: František Holubec
// Created: 20.10.2025

using System;
using System.Collections.Generic;

namespace EDIVE.Conditions
{
    public enum ComparisonType
    {
        GreaterOrEqualsTo,
        GreaterThan,
        EqualsTo,
        LessOrEqualsTo,
        LessThan,
        NotEqualsTo
    }

    public static class ComparisonTypeExtension
    {
        public static bool CompareValues<TVal>(this ComparisonType comparison, TVal a, TVal b, IComparer<TVal> comparer = null) where TVal : IComparable<TVal>
        {
            comparer ??= Comparer<TVal>.Default;
            var compareValue = comparer.Compare(a, b);
            return comparison switch
            {
                ComparisonType.EqualsTo => compareValue == 0,
                ComparisonType.GreaterThan => compareValue > 0,
                ComparisonType.LessThan => compareValue < 0,
                ComparisonType.GreaterOrEqualsTo => compareValue >= 0,
                ComparisonType.LessOrEqualsTo => compareValue <= 0,
                ComparisonType.NotEqualsTo => compareValue != 0,
                _ => throw new ArgumentOutOfRangeException()

            };
        }

        public static string ToOperatorString(this ComparisonType rule) => rule switch
        {
            ComparisonType.EqualsTo => "==",
            ComparisonType.GreaterThan => ">",
            ComparisonType.LessThan => "<",
            ComparisonType.GreaterOrEqualsTo => ">=",
            ComparisonType.LessOrEqualsTo => "<=",
            ComparisonType.NotEqualsTo => "!=",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
