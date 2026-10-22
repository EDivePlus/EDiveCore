// Author: František Holubec
// Created: 21.10.2026

namespace EDIVE.ServiceHub.Streaming.UI
{
    // A live source someone in the session can watch
    public readonly struct StreamEntry
    {
        public string Name { get; }
        public string Source { get; }

        public StreamEntry(string name, string source)
        {
            Name = name;
            Source = source;
        }
    }
}
