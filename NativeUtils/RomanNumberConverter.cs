using System.Collections.Generic;
using System.Text;

namespace EDIVE.NativeUtils
{
    public static class RomanNumberConverter
    {
        private static readonly Dictionary<int, string> TO_ROMAN_DICTIONARY = new Dictionary<int, string>
        {
            { 1000, "M" },
            { 900, "CM" },
            { 500, "D" },
            { 400, "CD" },
            { 100, "C" },
            { 90, "XC" },
            { 50, "L" },
            { 40, "XL" },
            { 10, "X" },
            { 9, "IX" },
            { 5, "V" },
            { 4, "IV" },
            { 1, "I" },
        };
        
        private static readonly Dictionary<char, int> TO_NUMBER_DICTIONARY = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 },
        };
        
        public static string ToRoman(int number)
        {
            var roman = new StringBuilder();

            foreach (var item in TO_ROMAN_DICTIONARY)
            {
                while (number >= item.Key)
                {
                    roman.Append(item.Value);
                    number -= item.Key;
                }
            }

            return roman.ToString();
        }

        public static int FromRoman(string roman)
        {
            var total = 0;
            var previousRoman = '\0';

            foreach (var currentRoman in roman)
            {
                var previous = previousRoman != '\0' ? TO_NUMBER_DICTIONARY[previousRoman] : '\0';
                var current = TO_NUMBER_DICTIONARY[currentRoman];

                if (previous != 0 && current > previous)
                {
                    total = total - (2 * previous) + current;
                }
                else
                {
                    total += current;
                }

                previousRoman = currentRoman;
            }

            return total;
        }
        
    }
}