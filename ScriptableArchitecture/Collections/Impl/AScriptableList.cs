// Author: František Holubec
// Created: 11.07.2025

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EDIVE.ScriptableArchitecture.Collections.Impl
{
    public abstract class AScriptableList : AScriptableBase
    {

    }

    public abstract class AScriptableList<T> : AScriptableList, IList<T>
    {
        [SerializeField] 
        protected List<T> _List = new();

        public event Action ItemsChanged;
        public event Action<IEnumerable<T>>  ItemsAdded;
        public event Action<IEnumerable<T>>  ItemsRemoved;

        public override Type GenericType => typeof(T);

        public int Count => _List.Count;
        public bool IsReadOnly => false;
        public bool IsEmpty => _List.Count == 0;

        public T this[int index]
        {
            get => _List[index];
            set
            {
                _List[index] = value;
                ItemsChanged?.Invoke();
            }
        }

        public void Add(T item)
        {
            _List.Add(item);
            ItemsChanged?.Invoke();
            ItemsAdded?.Invoke(new[] { item });
        }

        public bool TryAdd(T item)
        {
            if (_List.Contains(item))
                return false;

            Add(item);
            return true;
        }

        /// <summary>
        /// Adds a range of items to the list.
        /// Raises OnItemCountChanged and OnItemsAdded event once, after all items have been added.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
                return;

            var array = items.ToArray();
            if (array.Length == 0)
                return;

            _List.AddRange(array);
            ItemsChanged?.Invoke();
            ItemsAdded?.Invoke(array);
        }

        public bool TryAddRange(IEnumerable<T> items)
        {
            if (items == null)
                return false;

            var uniqueItems = items.Where(item => !_List.Contains(item)).ToList();
            if (uniqueItems.Count <= 0)
                return false;

            AddRange(uniqueItems);
            return true;
        }

        public void Insert(int index, T item)
        {
            _List.Insert(index, item);
            ItemsChanged?.Invoke();
            ItemsAdded?.Invoke(new[] { item });
        }

        public bool Remove(T item)
        {
            if (!_List.Remove(item))
                return false;

            ItemsChanged?.Invoke();
            ItemsRemoved?.Invoke(new[] { item });
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _List.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

            var value = _List[index];
            _List.RemoveAt(index);
            ItemsChanged?.Invoke();
            ItemsRemoved?.Invoke(new[] { value });
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0 || count < 0)
                return;

            if (index + count > _List.Count)
                return;

            var itemsToRemove = _List.GetRange(index, count);
            _List.RemoveRange(index, count);
            ItemsChanged?.Invoke();
            ItemsRemoved?.Invoke(itemsToRemove);
        }

        public bool Contains(T item)
        {
            return _List.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _List.IndexOf(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _List.CopyTo(array, arrayIndex);
        }

        public void Clear()
        {
            if (_List.Count == 0)
                return;

            var itemsToRemove = _List.ToArray();
            _List.Clear();
            ItemsChanged?.Invoke();
            ItemsRemoved?.Invoke(itemsToRemove);
        }

        public void ForEach(Action<T> action)
        {
            for (var i = _List.Count - 1; i >= 0; i--)
                action(_List[i]);
        }

        public IEnumerator<T> GetEnumerator() => _List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void Awake()
        {
            //Prevents from resetting if no reference in a scene
            hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        public override void ResetState()
        {
            _List.Clear();
            ItemsChanged = null;
            ItemsAdded = null;
            ItemsRemoved = null;
        }
    }
}
