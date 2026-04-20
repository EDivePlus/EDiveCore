// Author: František Holubec
// Created: 20.04.2026

using Spectre.Console.Rendering;

namespace EDIVE.Utils
{
    public readonly struct LogEntry
    {
        public readonly string Markup;
        public readonly IRenderable Renderable;
        public bool IsRenderable => Renderable != null;

        private LogEntry(string markup, IRenderable renderable)
        {
            Markup = markup;
            Renderable = renderable;
        }

        public static LogEntry FromMarkup(string markup) => new(markup, null);
        public static LogEntry FromRenderable(IRenderable r) => new(null, r);
    }
}
