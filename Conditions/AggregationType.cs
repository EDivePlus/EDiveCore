// Author: František Holubec
// Created: 20.10.2025

using System;

namespace EDIVE.Conditions
{
    public enum AggregationType
    {
        All,
        Any
    }
    
    public static class AggregationTypeExtension
    {
        public static string ToOperatorString(this AggregationType aggregation) => aggregation switch
        {
            AggregationType.All => "AND",
            AggregationType.Any => "OR",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
