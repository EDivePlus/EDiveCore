using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class CollectionExtensions
    {
        public static void InvokeAll(this List<System.Action> actList)
        {
            if (actList == null)
                return;

            foreach (var action in actList)
                action?.Invoke();
        }

        public static void SetCount<T>(this List<T> list, int count)
        {
            while (list.Count > count)
                list.RemoveAt(list.Count - 1);
            while (list.Count < count)
                list.Add(default);
        }

        public static bool IsIndexInRange<T>(this IReadOnlyList<T> list, int index)
        {
            if (list == null) return false;
            return 0 <= index && index < list.Count;
        }

        public static bool IsIndexInRange<T>(this T[] array, int index)
        {
            if (array == null) return false;
            return 0 <= index && index < array.Length;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return true;
            return !source.Any();
        }

        public static bool IsNullOrEmpty<T>(this IList<T> list) => list == null || list.Count == 0;


        public static bool IsOrdered<TSource, TKey>(this IList<TSource> list, Func<TSource, TKey> keySelector)
        {
            var comparer = Comparer<TKey>.Default;
            return IsOrdered(list, (first, second) => comparer.Compare(keySelector(first), keySelector(second)));
        }

        public static bool IsOrdered<T>(this IList<T> list, IComparer<T> comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            return IsOrdered(list, comparer.Compare);
        }

        public static bool IsOrdered<T>(this IList<T> list, Comparison<T> comparison)
        {
            if (list.Count <= 1) return true;
            for (var i = 1; i < list.Count; i++)
            {
                if (comparison.Invoke(list[i - 1], list[i]) > 0)
                    return false;
            }

            return true;
        }

        public static bool IsOrdered<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var comparer = Comparer<TKey>.Default;
            return IsOrdered(source, (first, second) => comparer.Compare(keySelector(first), keySelector(second)));
        }

        public static bool IsOrdered<T>(this IEnumerable<T> source, IComparer<T> comparer = null)
        {
            comparer ??= Comparer<T>.Default;
            return IsOrdered(source, comparer.Compare);
        }

        public static bool IsOrdered<T>(this IEnumerable<T> source, Comparison<T> comparison)
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
                return true;

            var first = enumerator.Current;
            while (enumerator.MoveNext())
            {
                var second = enumerator.Current;
                if (comparison.Invoke(first, second) > 0)
                    return false;
                first = second;
            }

            return true;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T value)
        {
            var index = 0;
            var comparer = EqualityComparer<T>.Default; // or pass in as a parameter
            foreach (var item in source)
            {
                if (comparer.Equals(item, value)) return index;
                index++;
            }

            return -1;
        }

        public static bool TryGetFirst<T>(this IEnumerable<T> source, Predicate<T> predicate, out T resultValue)
        {
            if (source == null || predicate == null)
            {
                resultValue = default;
                return false;
            }

            foreach (var element in source)
            {
                if (!predicate(element))
                    continue;

                resultValue = element;
                return true;
            }

            resultValue = default;
            return false;
        }

        public static bool TryGetFirstT<TResult, TSource>(this IEnumerable<TSource> source, out TResult resultValue) where TResult : TSource
        {
            if (source == null)
            {
                resultValue = default;
                return false;
            }

            foreach (var element in source)
            {
                if (element is not TResult tElement)
                    continue;

                resultValue = tElement;
                return true;
            }

            resultValue = default;
            return false;
        }

        public static bool TryGetFirstT<TResult, TSource>(this IEnumerable<TSource> source, Predicate<TResult> predicate, out TResult resultValue) where TResult : TSource
        {
            if (source == null || predicate == null)
            {
                resultValue = default;
                return false;
            }

            foreach (var element in source)
            {
                if (element is not TResult tElement || !predicate(tElement))
                    continue;

                resultValue = tElement;
                return true;
            }

            resultValue = default;
            return false;
        }


        public static bool TryGetFirst<TElement, TResult>(this IEnumerable<TElement> source, Predicate<TElement> predicate,
            Func<TElement, TResult> resultGetter, out TResult resultValue)
        {
            resultValue = default;
            if (source == null || predicate == null || resultGetter == null)
                return false;

            if (!source.TryGetFirst(predicate, out var resultElement))
                return false;

            resultValue = resultGetter.Invoke(resultElement);
            return true;
        }

        public static bool TryGetLast<T>(this IList<T> source, Predicate<T> predicate, out T resultValue)
        {
            if (source == null || predicate == null)
            {
                resultValue = default;
                return false;
            }

            for (var i = source.Count - 1; i >= 0; i--)
            {
                var element = source[i];
                if (!predicate(element))
                    continue;

                resultValue = element;
                return true;
            }

            resultValue = default;
            return false;
        }

        public static bool TryGetLast<TElement, TResult>(this IList<TElement> source, Predicate<TElement> predicate,
            Func<TElement, TResult> resultGetter, out TResult resultValue)
        {
            resultValue = default;
            if (source == null || predicate == null || resultGetter == null)
                return false;

            if (!source.TryGetLast(predicate, out var resultElement))
                return false;

            resultValue = resultGetter.Invoke(resultElement);
            return true;
        }


        public static bool TrySetValue<TElement>(this TElement[] source, int index, TElement element)
        {
            if (index < 0 || index >= source.Length)
                return false;

            source[index] = element;
            return true;
        }

        public static IEnumerable<TTarget> Filter<TTarget>(this IEnumerable source, Predicate<TTarget> filter = null)
        {
            foreach (var element in source)
            {
                if (element is TTarget tElement && (filter == null || filter(tElement)))
                    yield return tElement;
            }
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, Predicate<T> filter = null)
        {
            foreach (var element in source)
            {
                if (filter == null || filter(element))
                    yield return element;
            }
        }

        public static IEnumerable<T> FilterT<T, TParent>(this IEnumerable<T> source, Predicate<TParent> filter = null) where T : TParent
        {
            foreach (var element in source)
            {
                if (filter == null || filter(element))
                    yield return element;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var obj in source)
                action(obj);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var num = 0;
            foreach (var obj in source)
                action(obj, num++);
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source) { return source ?? Enumerable.Empty<T>(); }

        public static IList<T> Pad<T>(this IList<T> source, int count, T value = default)
        {
            if (source == null)
                return null;

            if (source.Count >= count)
                return source;

            var list = new List<T>(source);
            for (var i = source.Count; i < count; i++)
                list.Add(value);

            return list;
        }

        /// <summary>
        /// May be performance heavy, use in live app with caution!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="other"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool HasSameContentsAs<T>(this ICollection<T> source, ICollection<T> other)
        {
            if (source.Count != other.Count)
            {
                return false;
            }

            var s = source
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => x.Count());
            var o = other
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => x.Count());
            return s.Count == o.Count &&
                   s.All(x => o.TryGetValue(x.Key, out var count) && count == x.Value);
        }

        public static T GetClamp<T>(this IReadOnlyList<T> readOnlyList, int index)
        {
            if (readOnlyList == null) return default;
            if (index < 0) index = 0;
            else if (index >= readOnlyList.Count) index = readOnlyList.Count - 1;
            return readOnlyList[index];
        }

        public static bool TryGetClamp<T>(this IReadOnlyList<T> readOnlyList, int index, out T value)
        {
            if (readOnlyList == null)
            {
                value = default;
                return false;
            }

            value = readOnlyList.GetClamp(index);
            return true;
        }

        public static Tuple<List<T1>, List<T2>> Unpack<T1, T2>(this List<Tuple<T1, T2>> list)
        {
            var listA = new List<T1>(list.Count);
            var listB = new List<T2>(list.Count);
            foreach (var t in list)
            {
                listA.Add(t.Item1);
                listB.Add(t.Item2);
            }

            return Tuple.Create(listA, listB);
        }

        public static (List<T1>, List<T2>) Unpack<T1, T2>(this List<(T1, T2)> list)
        {
            var listA = new List<T1>(list.Count);
            var listB = new List<T2>(list.Count);
            foreach (var t in list)
            {
                listA.Add(t.Item1);
                listB.Add(t.Item2);
            }

            return (listA, listB);
        }

        public static List<Tuple<T1, T2>> Pack<T1, T2>(this List<T1> listA, List<T2> listB)
        {
            var list = new List<Tuple<T1, T2>>(listA.Count);
            list.AddRange(listA.Select((t, i) => Tuple.Create(t, listB[i])));

            return list;
        }

        public static void Normalize(this float[,] array)
        {
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;

            var width = array.GetLength(0);
            var height = array.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (array[x, y] > max) max = array[x, y];
                    if (array[x, y] < min) min = array[x, y];
                }
            }

            array.Remap(min, max, 0, 1);
        }

        public static void Normalize(this double[,] array)
        {
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;

            var width = array.GetLength(0);
            var height = array.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (array[x, y] > max) max = array[x, y];
                    if (array[x, y] < min) min = array[x, y];
                }
            }

            array.Remap(min, max, 0, 1);
        }

        public static float[,] Transpose(this float[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var result = new float[cols, rows];

            for (var i = 0; i < rows; i++)
            {
                for (var j = 0; j < cols; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }

            return result;
        }

        public static float[,] ExtendByOne(this float[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);

            var extended = new float[rows + 1, cols + 1];

            for (var i = 0; i < rows; i++)
            for (var j = 0; j < cols; j++)
                extended[i, j] = matrix[i, j];

            for (var j = 0; j < cols; j++)
                extended[rows, j] = matrix[rows - 1, j];

            for (var i = 0; i < rows; i++)
                extended[i, cols] = matrix[i, cols - 1];

            extended[rows, cols] = matrix[rows - 1, cols - 1];
            return extended;
        }

        public static void Remap(this double[,] array, double inputMin, double inputMax, double targetMin, double targetMax)
        {
            var width = array.GetLength(0);
            var height = array.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    array[x, y] = MathExtensions.Remap(array[x, y], inputMin, inputMax, targetMin, targetMax);
                }
            }
        }

        public static void Remap(this float[,] array, float inputMin, float inputMax, float targetMin, float targetMax)
        {
            var width = array.GetLength(0);
            var height = array.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (float.IsNaN(array[x, y]))
                    {
                        array[x, y] = 0;
                    }
                    array[x, y] = MathExtensions.Remap(array[x, y], inputMin, inputMax, targetMin, targetMax);
                }
            }
        }

        public static void Clamp(this float[,] array, float min, float max)
        {
            var width = array.GetLength(0);
            var height = array.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (float.IsNaN(array[x, y]))
                    {
                        array[x, y] = 0;
                    }
                    array[x, y] = Mathf.Clamp(array[x, y], min, max);
                }
            }
        }

        public static float[,] ToFloat(this double[,] inputArray)
        {
            return inputArray.Convert(value => (float) value);
        }

        public static TResult[,] Apply<TSource, TResult>(this TSource[,] inputArray, Func<TSource[,], int, int, TResult> function)
        {
            if (inputArray == null)
                return null;

            var width = inputArray.GetLength(0);
            var height = inputArray.GetLength(1);

            var output = new TResult[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    output[x, y] = function(inputArray, x, y);
                }
            }

            return output;
        }

        public static TResult[,] Convert<TSource, TResult>(this TSource[,] inputArray, Func<TSource, TResult> convertFunction)
        {
            if (inputArray == null)
                return null;

            var width = inputArray.GetLength(0);
            var height = inputArray.GetLength(1);

            var output = new TResult[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    output[x, y] = convertFunction(inputArray[x, y]);
                }
            }

            return output;
        }

        public static T[,] Resize2D<T>(this T[,] original, int rows, int cols)
        {
            var newArray = new T[rows, cols];
            var minRows = Math.Min(rows, original.GetLength(0));
            var minCols = Math.Min(cols, original.GetLength(1));
            for(var i = 0; i < minRows; i++)
            for(var j = 0; j < minCols; j++)
                newArray[i, j] = original[i, j];
            return newArray;
        }

        public static bool IsValid2DCoordinate<T>(this T[,] array, int x, int y)
        {
            return x >= 0 && x < array.GetLength(0) && y >= 0 && y < array.GetLength(1);
        }

        public static T[] To1DArray<T>(this T[,] input)
        {
            var width = input.GetLength(0);
            var height = input.GetLength(1);

            var result = new T[input.Length];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    result[y + x * height] = input[x, y];
                }
            }
            return result;
        }
    }
}
