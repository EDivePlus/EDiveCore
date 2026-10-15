// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using System.Linq;

namespace EDIVE.Debugging
{
    // Panel (root) or page. Created on first use, shown while it or a child page has controls.
    public sealed class DebugPage
    {
        private readonly List<DebugPage> _children = new();
        private readonly List<DebugSectionNode> _sections = new();
        private readonly DebugScope _scope;

        public string Name { get; }
        public string Path { get; }
        public DebugPage Parent { get; }
        public string Title { get; private set; }
        public string IconName { get; private set; }
        public int SortOrder { get; private set; }
        public bool IsPanel => Parent == null;
        internal int Sequence { get; }

        public bool HasContent => _sections.Any(s => s.Controls.Count > 0) || _children.Any(c => c.HasContent);

        internal IEnumerable<DebugPage> VisibleChildren => _children
            .Where(c => c.HasContent)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Sequence);

        internal IReadOnlyList<DebugPage> Children => _children;
        internal IReadOnlyList<DebugSectionNode> Sections => _sections;

        internal IEnumerable<DebugSectionNode> VisibleSections => _sections
            .Where(s => s.Controls.Count > 0)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Sequence);

        internal DebugPage(string name, DebugPage parent, DebugScope scope)
        {
            Name = name;
            Title = name;
            Parent = parent;
            Path = parent == null ? name : parent.Path + DebugMenu.PAGE_SEPARATOR + name;
            Sequence = DebugMenu.NextSequence();
            _scope = scope;
        }

        // Page that is not part of the menu tree, used by tools to reuse the section layout.
        public static DebugPage CreateDetached(string name) => new(name, null, null);

        public DebugPage Icon(string iconName)
        {
            IconName = iconName;
            DebugMenu.NotifyChanged();
            return this;
        }

        public DebugPage Order(int order)
        {
            SortOrder = order;
            DebugMenu.NotifyChanged();
            return this;
        }

        public DebugPage WithTitle(string title)
        {
            Title = string.IsNullOrEmpty(title) ? Name : title;
            DebugMenu.NotifyChanged();
            return this;
        }

        // Section handle without owner on this page.
        public DebugSection Section(string name = null) => new(_scope, GetOrCreateSection(name));

        public void Refresh()
        {
            foreach (var section in _sections)
            {
                foreach (var control in section.Controls)
                {
                    control.Refresh();
                }
            }

            foreach (var child in _children)
            {
                child.Refresh();
            }
        }

        internal DebugPage GetOrCreateChild(string name)
        {
            var child = FindChild(name);
            if (child != null)
                return child;

            child = new DebugPage(name, this, _scope);
            _children.Add(child);
            return child;
        }

        internal DebugPage FindChild(string name) => _children.FirstOrDefault(c => c.Name == name);

        internal DebugSectionNode GetOrCreateSection(string name)
        {
            name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            var section = _sections.FirstOrDefault(s => s.Name == name);
            if (section != null)
                return section;

            section = new DebugSectionNode(this, name);
            _sections.Add(section);
            return section;
        }
    }
}
