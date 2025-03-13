using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace EDIVE.Extensions.Random
{
    public class WeightedRandomBag
    {
        private IRandom _randomGenerator;
        public IRandom RandomGenerator
        {
            get => _randomGenerator ??= new UnityRandom();
            set => _randomGenerator = value;
        }

        protected WeightedRandomBag()
        {
            _randomGenerator = new UnityRandom();
        }
        
        protected WeightedRandomBag(IRandom randomGenerator)
        {
            _randomGenerator = randomGenerator;
        }
    }
    
    public class WeightedRandomBag<T> : WeightedRandomBag
    {
        public struct Entry : IEquatable<Entry>
        {
            public float AccumulatedWeight;
            public float Weight;
            public T Item;

            public bool Equals(Entry other)
            {
                return EqualityComparer<T>.Default.Equals(Item, other.Item);
            }

            public override bool Equals(object obj)
            {
                return obj is Entry other && Equals(other);
            }

            public override int GetHashCode()
            {
                return EqualityComparer<T>.Default.GetHashCode(Item);
            }
        }

        public int Count => _entries.Count;
        
        private List<Entry> _entries = new List<Entry>();
        private float _accumulatedWeight;
        
        public WeightedRandomBag() { }
        
        public WeightedRandomBag(IRandom randomGenerator) : base(randomGenerator)  { }
        
        public WeightedRandomBag(IEnumerable<T> values, WeightGetter<T> weightGetter, ValueFilter<T> filter = null) 
        {
            foreach (var value in values)
            {
                if (filter != null && !filter(value)) continue;
                AddEntry(value, weightGetter(value));
            }
        }
               
        public WeightedRandomBag(IRandom randomGenerator, IEnumerable<T> values, WeightGetter<T> weightGetter, ValueFilter<T> filter = null) : base(randomGenerator) 
        {
            foreach (var value in values)
            {
                if (filter != null && !filter(value)) continue;
                AddEntry(value, weightGetter(value));
            }
        }

        public static WeightedRandomBag<T> GenerateFrom<TWrapper>(IEnumerable<TWrapper> values, ValueGetter<TWrapper, T> valueGetter, 
            WeightGetter<TWrapper> weightGetter, ValueFilter<T> filter = null)
        {
            var bag = new WeightedRandomBag<T>(); 
            foreach (var wrapper in values)
            {
                if(wrapper == null)
                    continue;

                var value = valueGetter(wrapper);
                if (filter != null && !filter(value)) continue;
                bag.AddEntry(value, weightGetter(wrapper));
            }
            return bag;
        }

        public static WeightedRandomBag<T> GenerateFrom<TWrapper>(IRandom randomGenerator, IEnumerable<TWrapper> values, ValueGetter<TWrapper, T> valueGetter, 
            WeightGetter<TWrapper> weightGetter, ValueFilter<T> filter = null)
        {
            var bag = new WeightedRandomBag<T>(randomGenerator); 
            foreach (var wrapper in values)
            {
                if(wrapper == null)
                    continue;

                var value = valueGetter(wrapper);
                if (filter != null && !filter(value)) continue;
                bag.AddEntry(value, weightGetter(wrapper));
            }
            return bag;
        }
        
        public void AddEntry(T item, float weight)
        {
            _accumulatedWeight += weight;
            _entries.Add(new Entry
            {
                Item = item, 
                AccumulatedWeight = _accumulatedWeight,
                Weight = weight
            });
        }
        
        public void AddRange(IEnumerable<T> items, IEnumerable<float> weights)
        {
            using var weightEnumerator = weights.GetEnumerator();
            foreach (var item in items)
            {
                weightEnumerator.MoveNext();
                AddEntry(item, weightEnumerator.Current);
            }
        }
        
        public void AddRange(IEnumerable<T> items, WeightGetter<T> weightGetter)
        {
            foreach (var item in items)
            {
                AddEntry(item, weightGetter(item));
            }
        }

        public void RemoveEntry(Entry entry)
        {
            _entries.Remove(entry);
            _accumulatedWeight = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                var entryItem = _entries[i];
                _accumulatedWeight += entry.Weight;
                entryItem.AccumulatedWeight = _accumulatedWeight;
                _entries[i] = entryItem;
            }
        }
        
        public bool TryRemoveEntry(T item)
        {
            Entry? foundItem = null;
            foreach (var entry in _entries)
            {
                if (!entry.Item.Equals(item)) continue;
                foundItem = entry;
                break;
            }
            if (foundItem == null) return false;
            RemoveEntry(foundItem.Value);
            return true;
        }
        
        public bool TryGetRandom(out T result)
        {
            var r = RandomGenerator.Next() * _accumulatedWeight;
            foreach (var entry in _entries)
            {
                if (entry.AccumulatedWeight >= r)
                {
                    result = entry.Item;
                    return true;
                }
            }

            //should only happen when there are no entries
            result = default;
            return false; 
        }
        
        public T GetRandom()
        {
            TryGetRandom(out var result);
            return result;
        }
        
        public bool TryGetRandomAndRemove(out T result)
        {
            var r = RandomGenerator.Next() * _accumulatedWeight;
            foreach (var entry in _entries)
            {
                if (entry.AccumulatedWeight >= r)
                {
                    var a = entry.Item;
                    RemoveEntry(entry);
                    result = a;
                    return true;
                }
            }

            //should only happen when there are no entries
            result = default;
            return false;
        }
        
        [CanBeNull]
        public T GetRandomAndRemove()
        {
            TryGetRandomAndRemove(out var result);
            return result;
        }
    }
    
    public delegate bool ValueFilter<in TValue>(TValue value);
    public delegate float WeightGetter<in TValue>(TValue value);
    public delegate TValue ValueGetter<in TWrapper, out TValue>(TWrapper wrapper);
        
}