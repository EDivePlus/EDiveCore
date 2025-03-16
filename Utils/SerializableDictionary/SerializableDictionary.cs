using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Utils.SerializableDictionary
{
    public abstract class SerializableDictionary
    {
        public abstract IDictionary GetBackingDictionary();
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionary, IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("list")]
        [FormerlySerializedAs("_list")]
        [FormerlySerializedAs("backingList")]
        [FormerlySerializedAs("backingDictionary")]
        [SerializeField]
        private List<SerializableKeyValuePair> _List = new List<SerializableKeyValuePair>();
        
        private Dictionary<TKey, int> _keyIndexes = new Dictionary<TKey, int>();
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();
        
        [ReadOnly]
        [UsedImplicitly]
        [ShowInInspector]
        private bool _keyCollision;
        
        [Serializable]
        public struct SerializableKeyValuePair
        {
            [FormerlySerializedAs("key")]
            [SerializeField]
            private TKey _Key;

            [FormerlySerializedAs("value")]
            [SerializeField]
            private TValue _Value;

            public TKey Key => _Key;
            public TValue Value => _Value;

            public SerializableKeyValuePair(TKey tKey, TValue tValue)
            {
                _Key = tKey;
                _Value = tValue;
            }
        }
        
        public SerializableDictionary() { }
        
        public SerializableDictionary(SerializableDictionary<TKey, TValue> dictionary) : this()
        {
            _List = dictionary._List;
            _keyIndexes = dictionary._keyIndexes;
            _dict = dictionary._dict;
            _keyCollision = dictionary._keyCollision;
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : this()
        {
            CopyValuesFrom(dictionary);
        }
        
        public SerializableDictionary<TKey, TValue> GetCopy() => new SerializableDictionary<TKey, TValue>(this);
        
        public static implicit operator SerializableDictionary<TKey, TValue> (Dictionary<TKey, TValue> dictionary)
        {
            return dictionary?.ToSerializable();
        }
        
        public static implicit operator Dictionary<TKey, TValue>(SerializableDictionary<TKey, TValue> serializableDictionary)
        {
            return serializableDictionary?._dict;
        }
        
        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            _dict.Clear();
            _keyIndexes.Clear();
            _keyCollision = false;
            UpdateDictFromList();
        }

        private void UpdateDictFromList()
        {
            for (var i = 0; i < _List.Count; i++)
            {
                var key = _List[i].Key;
                if (key != null && !ContainsKey(key))
                {
                    _dict.Add(key, _List[i].Value);
                    _keyIndexes.Add(key, i);
                }
                else
                {
                    _keyCollision = true;
                }
            }
        }

        public TValue this[[CanBeNull] TKey key]
        {
            get => key != null ? _dict[key] : default;
            set
            {
                if(key == null) return;
                _dict[key] = value;

                if (_keyIndexes.TryGetValue(key, out var index))
                {
                    _List[index] = new SerializableKeyValuePair(key, value);
                }
                else
                {
                    _List.Add(new SerializableKeyValuePair(key, value));
                    _keyIndexes.Add(key, _List.Count - 1);
                }
            }
        }

        public ICollection<TKey> Keys => _dict.Keys;
        public ICollection<TValue> Values => _dict.Values;

        public void Add([CanBeNull] TKey key, TValue value)
        {
            if(key == null) return;
            _dict.Add(key, value);
            _List.Add(new SerializableKeyValuePair(key, value));
            _keyIndexes.Add(key, _List.Count - 1);
        }

        public bool ContainsKey([CanBeNull] TKey key)
        {
            return key != null && _dict.ContainsKey(key);
        }

        public bool Remove([CanBeNull] TKey key)
        {
            if(key == null) return false;
            if (_dict.Remove(key))
            {
                var index = _keyIndexes[key];
                _List.RemoveAt(index);
                UpdateKeyIndexes(index);
                _keyIndexes.Remove(key);
                return true;
            }
            return false;
        }

        private void UpdateKeyIndexes(int removedIndex) 
        {
            for (var i = removedIndex; i < _List.Count; i++) 
            {
                var key = _List[i].Key;
                _keyIndexes[key]--;
            }
        }
        
        private void UpdateKeyIndexes()
        {
            var numEntries = _List.Count;
            _keyIndexes ??= new Dictionary<TKey, int>(numEntries);
            _keyIndexes.Clear();
            for (var i = 0; i < numEntries; i++)
            {
                _keyIndexes[_List[i].Key] = i;
            }
        }
        
        public bool TryGetValue([CanBeNull] TKey key, out TValue value)
        {
            if (key != null) 
                return _dict.TryGetValue(key, out value);
            
            value = default;
            return false;
        }

        // ICollection
        public int Count => _dict.Count;
        public bool IsReadOnly { get; set; }

        public void Add(KeyValuePair<TKey, TValue> pair)
        {
            Add(pair.Key, pair.Value);
        }

        public void Clear()
        {
            _dict.Clear();
            _List.Clear();
            _keyIndexes.Clear();
        }
        
        public void ClearNullKeys()
        {
            for (var i = _List.Count - 1; i >= 0; i--)
            {
                // Unity fakes null for UnityEngine.Object so we need to cast it 
                if (_List[i].Key == null || (_List[i].Key is UnityEngine.Object obj && obj == null))
                {
                    _List.RemoveAt(i);
                }
                UpdateKeyIndexes(i);
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return _dict.TryGetValue(pair.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, pair.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentException("The array cannot be null.");
            if (arrayIndex < 0)
               throw new ArgumentOutOfRangeException(nameof(arrayIndex), "The starting array index cannot be negative.");
            if (array.Length - arrayIndex < _dict.Count)
                throw new ArgumentException("The destination array has fewer elements than the collection.");

            foreach (var pair in _dict)
            {
                array[arrayIndex] = pair;
                arrayIndex++;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> pair)
        {
            if (_dict.TryGetValue(pair.Key, out var value))
            {
                var valueMatch = EqualityComparer<TValue>.Default.Equals(value, pair.Value);
                if (valueMatch)
                {
                    return Remove(pair.Key);
                }
            }
            return false;
        }
        
        public SerializableDictionary CopyValuesFrom(IDictionary<TKey, TValue> dictionary)
        {
            _List ??= new List<SerializableKeyValuePair>();
            _dict ??= new Dictionary<TKey, TValue>(dictionary);
            _keyIndexes ??= new Dictionary<TKey, int>();
            _keyCollision = false;

            if (dictionary != null)
            {
                foreach (var keyValuePair in dictionary)
                {
                    _List.Add(new SerializableKeyValuePair(keyValuePair.Key, keyValuePair.Value));
                }
            }

            UpdateDictFromList();

            return this;
        }

        public override IDictionary GetBackingDictionary() => _dict;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }

    public static class SerializableDictionaryExtensions
    {
        public static SerializableDictionary<TKey, TValue> ToSerializable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return new SerializableDictionary<TKey, TValue>(dictionary);
        }
    }
}
