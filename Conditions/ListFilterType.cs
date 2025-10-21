// Author: František Holubec
// Created: 21.10.2025

using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIVE.Conditions
{
    public enum ListFilterType
    {
        Whitelist,
        Blacklist,
    }
    
    public static class ListFilterTypeExtensions
    {
        public static bool Filter<T>(this IEnumerable<T> collection, T value, ListFilterType filterType)
        {
            return filterType switch
            {
                ListFilterType.Blacklist => !collection.Contains(value),
                ListFilterType.Whitelist => collection.Contains(value),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
