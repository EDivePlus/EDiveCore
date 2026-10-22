// Author: František Holubec
// Created: 20.10.2026

namespace EDIVE.ServiceHub.Streaming.Sources
{
    public readonly struct StreamViewer
    {
        public string Identity { get; }
        public string Name { get; }

        // app, guest or web
        public string Kind { get; }

        public bool IsWeb => Kind == "web";

        public StreamViewer(string identity, string name, string kind)
        {
            Identity = identity;
            Name = name;
            Kind = kind;
        }
    }
}
