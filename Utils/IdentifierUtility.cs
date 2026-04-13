// Author: František Holubec
// Created: 08.04.2026

using System;
using System.Collections.Generic;
using System.Text;
using ZLinq;

namespace EDIVE.Utils
{
    public static class IdentifierUtility
    {
        public static string GeneratePrefixedNumericID<T>(T self, IEnumerable<T> parentCollection, Func<T, string> idGetter, string prefix = "")
        {
            if (idGetter == null)
                return null;
            
            var highestID = parentCollection.AsValueEnumerable()
                .Where(e => e != null && !ReferenceEquals(e, self) && idGetter.Invoke(e).StartsWith(prefix) && int.TryParse(idGetter.Invoke(e)[1..], out _))
                .Select(e => int.Parse(idGetter.Invoke(e)[1..]))
                .Prepend(0)
                .Max();
            return $"{prefix}{highestID + 1:D2}";    
        }
        
        public static string GenerateAlphaID<T>(T self, IEnumerable<T> parentCollection, Func<T, string> idGetter)
        {
            if (idGetter == null)
                return null;
            
            var highestIndex = parentCollection.AsValueEnumerable()
                .Where(e => e != null && !ReferenceEquals(e, self))
                .Select(e => TryParseAlphaId(idGetter.Invoke(e), out var index) ? index : -1)
                .Where(i  => i >= 0)
                .Prepend(-1)
                .Max();

            return ToAlphaId(highestIndex + 1);
        }
        
        public static bool TryParseAlphaId(string id, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(id) || id.AsValueEnumerable().Any(c => c is < 'A' or > 'Z'))
                return false;

            var value = id.AsValueEnumerable().Aggregate(0, (current, c) => current * 26 + (c - 'A') + 1);
            index = value - 1;
            return true;
        }

        public static string ToAlphaId(int index)
        {
            if (index < 0) 
                return "A";

            var n = index + 1;
            var buffer = new StringBuilder();

            while (n > 0)
            {
                var remainder = (n - 1) % 26;
                buffer.Insert(0, (char)('A' + remainder));
                n = (n - 1) / 26;
            }

            return buffer.ToString();
        }
    }
}
