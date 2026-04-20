using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.Utils
{
    public sealed class ConsoleCommand
    {
        public string Name { get; }
        public string Description { get; }
        public Func<string[], UniTask> Handler { get; }

        public ConsoleCommand(string name, string description, Func<string[], UniTask> handler)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name.Trim().ToLowerInvariant();
            Description = description ?? "";
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public ConsoleCommand(string name, string description, Action<string[]> handler) : this(name, description, args =>
        {
            handler(args);
            return UniTask.CompletedTask;
        }) { }
    }
}
