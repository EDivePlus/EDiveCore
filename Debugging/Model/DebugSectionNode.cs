// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using System.Linq;

namespace EDIVE.Debugging
{
    // Shared section on a page. Several handles from different owners can add into it.
    internal sealed class DebugSectionNode
    {
        public DebugPage Page { get; }
        public string Name { get; }
        public int SortOrder { get; set; }
        public bool Collapsed { get; set; }
        public int Sequence { get; }
        public List<ADebugControl> Controls { get; } = new();

        public bool IsUntitled => Name == null;

        public IEnumerable<ADebugControl> SortedControls => Controls
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Sequence);

        public DebugSectionNode(DebugPage page, string name)
        {
            Page = page;
            Name = name;
            Sequence = DebugMenu.NextSequence();
            // Untitled section goes first unless ordered otherwise
            SortOrder = name == null ? int.MinValue : 0;
        }
    }
}
