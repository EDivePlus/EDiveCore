using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EDIVE.Extensions.Random
{
    public static class RandomUtility
    {
        public static readonly IRandom UNITY_RANDOM = new UnityRandom();
        public static readonly IRandom SYSTEM_RANDOM = new SystemRandom();
        
        public static IRandom GlobalRandom { get; set; } = UNITY_RANDOM;
        
        public static T RandomItem<T>(this IList<T> items, IRandom random = null)
        {
            if (items == null || items.Count == 0) 
                return default;
            
            random ??= GlobalRandom;
            return items[random.NextInt(0, items.Count)];
        }
        
        public static T RandomItem<T>(this T[] items, IRandom random = null)
        {
            if (items == null || items.Length == 0) 
                return default;

            random ??= GlobalRandom;
            return items[random.NextInt(0, items.Length)];
        }
        
        public static T RandomItem<T>(this IEnumerable<T> items, IRandom random = null)
        {
            if (items == null) 
                return default;
            var count = items.Count();
            if (count == 0) 
                return default;

            random ??= GlobalRandom;
            var index = random.NextInt(0, count);
            return items.ElementAt(index);
        }

        public static bool TryPopRandomItem<T>(this IList<T> items, out T result, IRandom random = null)
        {
            result = default;
            if (items == null || items.Count == 0)
                return false;

            random ??= GlobalRandom;
            var count = items.Count();
            var index = random.NextInt(0, count);
            result = items[index];
            items.RemoveAt(index);
            return true;
        }
        
        public static void Shuffle<T>(IList<T> list, IRandom random = null)
        {
            random ??= GlobalRandom;
            if (list.Count <= 1) 
                return;
            
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var tmp = list[i];
                var randomIndex = random.NextInt(i + 1);

                //Swap elements
                list[i] = list[randomIndex];
                list[randomIndex] = tmp;
            }
        }

        public static T RandomWeightedItem<T>(this List<T> items, Func<T, float> weightGetter, IRandom random = null)
        {
            if (items == null || items.Count == 0) return default;
            return items[RandomWeightedIndex(items, weightGetter, random)];
        }

        public static int RandomWeightedIndex<T>(this List<T> items, Func<T, float> weightGetter, IRandom random = null)
        {
            if (items == null || items.Count == 0 || weightGetter == null) return 0;
            random ??= GlobalRandom;

            float sum = 0;
            foreach (var item in items)
            {
                var weight = weightGetter.Invoke(item);
                sum += weight;
            }

            var randomNum = random.NextFloat(0, sum);
            float currentCumulativeWeight = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var weight = weightGetter.Invoke(item);
                currentCumulativeWeight += weight;
                if (randomNum < currentCumulativeWeight)
                    return i;
            }

            return items.Count - 1;
        }

        public static IEnumerable<T> RandomItemEnumerate<T>(IReadOnlyList<T> items, IRandom random = null)
        {
            if (items == null || items.Count == 0) 
                yield break;

            var restLength = items.Count;
            var indices = new ushort[restLength];
            for (ushort i = 0; i < restLength; i++)
            {
                indices[i] = i;
            }

            random ??= GlobalRandom;

            while (restLength > 0)
            {
                var arrayIndex = random.NextInt(0, restLength);
                var itemIndex = indices[arrayIndex];

                yield return items[itemIndex];

                restLength--;
                indices[arrayIndex] = indices[restLength];
            }
        }

        public static float NextGaussian(float mu = 0, float sigma = 1, IRandom r = null)
        {
            r ??= GlobalRandom;
            var u1 = r.NextFloat();
            var u2 = r.NextFloat();

            var randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            var randNormal = mu + sigma * randStdNormal;

            return randNormal;
        }

        public static void NextSphericalRandom(ref Vector3 normalizedPoint)
        {
            float x, y, z;
            do
            {
                x = NextGaussian();
                y = NextGaussian();
                z = NextGaussian();
            } while (x == 0 && y == 0 && z == 0);

            var normalization = 1 / Mathf.Sqrt(x * x + y * y + z * z);

            normalizedPoint.x = x * normalization;
            normalizedPoint.y = y * normalization;
            normalizedPoint.z = z * normalization;
        }
    }
}
