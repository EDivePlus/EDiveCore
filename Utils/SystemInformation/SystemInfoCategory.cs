// Author: František Holubec
// Created: 03.06.2026

using System.Collections.Generic;
using EDIVE.NativeUtils;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoCategory 
    {
        public string Name { get; }

        private readonly List<SystemInfoEntry> _entries = new();
        public IReadOnlyList<SystemInfoEntry> Entries => _entries;

        public SystemInfoCategory(string name)
        {
            Name = name;
        }
        
        public bool TryGet(string entryName, out SystemInfoEntry value)
        {
            return _entries.TryGetFirst(e => e.Name == entryName, out value);
        }
        
        public void Add(SystemInfoEntry entry)
        {
            _entries.RemoveAll(e => e.Name == entry.Name);
            _entries.Add(entry);
            _entries.Sort();
        }

        public void AddRange(IEnumerable<SystemInfoEntry> entries)
        {
            foreach (var entry in entries)
            {
                Add(entry);
            }
        }
    }
}
