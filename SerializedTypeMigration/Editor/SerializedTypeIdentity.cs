using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIVE.SerializedTypeMigration.Editor
{
    // class/ns/asm as Unity writes it. nested class: Outer/Inner
    public readonly struct SerializedTypeIdentity : IEquatable<SerializedTypeIdentity>
    {
        public const string ARGUMENTS_START = "[[";

        public string Class { get; }
        public string Namespace { get; }
        public string Assembly { get; }

        public SerializedTypeIdentity(string className, string ns, string assembly)
        {
            Class = className ?? string.Empty;
            Namespace = ns ?? string.Empty;
            Assembly = assembly ?? string.Empty;
        }

        public bool IsEmpty => string.IsNullOrEmpty(Class);
        public bool IsInflated => Class.Contains(ARGUMENTS_START);
        public string FullName => Namespace.Length == 0 ? Class : $"{Namespace}.{Class}";
        public string GenericArguments => SplitArguments(Class).arguments;

        public SerializedTypeIdentity Open() => new(SplitArguments(Class).open, Namespace, Assembly);

        // null keeps the current part
        public SerializedTypeIdentity Override(string className, string ns, string assembly) =>
            new(className ?? Class, ns ?? Namespace, assembly ?? Assembly);

        // generic arguments and reflection nest with '+', Unity YAML with '/'
        public string ToGenericArgument() => $"{FullName.Replace('/', '+')}, {Assembly}";
        public string ToReflectionName() => Open().FullName.Replace('/', '+');

        // prefab/scene overrides write "<asm> <full name>"
        public string ToOverrideValue() => $"{Assembly} {FullName}";

        public static SerializedTypeIdentity FromOverrideValue(string value)
        {
            var space = value.IndexOf(' ');
            return space < 0 ? default : FromFullName(value[(space + 1)..], value[..space]);
        }

        public static SerializedTypeIdentity FromFullName(string fullName, string assembly, char nesting = '/')
        {
            var (open, arguments) = SplitArguments(fullName);

            // last dot = end of namespace. class part has no dots: nesting is '/' or '+', arguments cut off above
            var lastDot = open.LastIndexOf('.');
            var ns = lastDot < 0 ? string.Empty : open[..lastDot];
            var className = open[(lastDot + 1)..].Replace(nesting, '/');

            return new SerializedTypeIdentity(className + arguments, ns, assembly);
        }

        public static SerializedTypeIdentity FromType(Type type)
        {
            var className = type.Name;
            for (var outer = type.DeclaringType; outer != null; outer = outer.DeclaringType)
                className = $"{outer.Name}/{className}";

            return new SerializedTypeIdentity(className, type.Namespace, type.Assembly.GetName().Name);
        }

        public static (string open, string arguments) SplitArguments(string name)
        {
            var start = name.IndexOf(ARGUMENTS_START, StringComparison.Ordinal);
            return start < 0 ? (name, string.Empty) : (name[..start], name[start..]);
        }

        public bool Equals(SerializedTypeIdentity other) =>
            Class == other.Class && Namespace == other.Namespace && Assembly == other.Assembly;

        public override bool Equals(object obj) => obj is SerializedTypeIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Class, Namespace, Assembly);
        public override string ToString() => $"{FullName}, {Assembly}";

        public static bool operator ==(SerializedTypeIdentity left, SerializedTypeIdentity right) => left.Equals(right);
        public static bool operator !=(SerializedTypeIdentity left, SerializedTypeIdentity right) => !left.Equals(right);
    }

    // Generic class name as Unity writes it:
    //   Boxed`1[[Game.Item, Game]]                      `1 = one type parameter, [[..]] = the arguments
    //   Pair`2[[Game.A, Game],[Game.B, Game]]           one [Full.Name, Assembly] per argument
    //   Boxed`1[[Game.Pair`2[[Game.A, Game],[Game.B, Game]], Game]]   an argument can be generic too
    // Two things can move: the generic type itself ("Boxed`1") or any argument ("Game.Item"), at any depth.
    // Parse splits it into Open ("Boxed`1") + Arguments, each argument a full identity that resolves on its own.
    public sealed class GenericTypeName
    {
        public string Open { get; }
        public IReadOnlyList<GenericArgument> Arguments { get; }

        private GenericTypeName(string open, IReadOnlyList<GenericArgument> arguments)
        {
            Open = open;
            Arguments = arguments;
        }

        // null when malformed, caller leaves the text alone
        public static GenericTypeName Parse(string className)
        {
            var (open, rest) = SerializedTypeIdentity.SplitArguments(className);
            if (rest.Length == 0)
                return new GenericTypeName(open, Array.Empty<GenericArgument>());

            // rest is "[" + "[arg]" + ("," + "[arg]")* + "]"
            var arguments = new List<GenericArgument>();
            var i = 1;
            while (i < rest.Length && rest[i] == '[')
            {
                var close = FindClosingBracket(rest, i);
                var argument = close < 0 ? null : GenericArgument.Parse(rest[(i + 1)..close]);
                if (argument == null)
                    return null;

                arguments.Add(argument);
                i = close + 1;

                if (i == rest.Length - 1 && rest[i] == ']')
                    return new GenericTypeName(open, arguments);
                if (i >= rest.Length || rest[i] != ',')
                    return null;
                i++;
            }
            return null;
        }

        public static string Compose(string open, IEnumerable<GenericArgument> arguments)
        {
            var written = arguments.Select(argument => $"[{argument.Write()}]").ToList();
            return written.Count == 0 ? open : $"{open}[{string.Join(",", written)}]";
        }

        private static int FindClosingBracket(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '[':
                        depth++;
                        break;
                    case ']' when --depth == 0:
                        return i;
                }
            }
            return -1;
        }
    }

    // one argument: "Game.Outer+Inner, Game" plus anything after the assembly (", Version=..") kept as Tail
    public sealed class GenericArgument
    {
        public SerializedTypeIdentity Identity { get; }
        public string Tail { get; }

        public GenericArgument(SerializedTypeIdentity identity, string tail)
        {
            Identity = identity;
            Tail = tail ?? string.Empty;
        }

        public GenericArgument With(SerializedTypeIdentity identity) => new(identity, Tail);
        public string Write() => $"{Identity.ToGenericArgument()}{Tail}";

        public static GenericArgument Parse(string text)
        {
            // commas inside nested [[..]] belong to the inner arguments
            var comma = FirstTopLevelComma(text);
            if (comma < 0)
                return null;

            var typeName = text[..comma].Trim();
            var rest = text[(comma + 1)..];
            var assemblyEnd = FirstTopLevelComma(rest);
            var assembly = (assemblyEnd < 0 ? rest : rest[..assemblyEnd]).Trim();
            var tail = assemblyEnd < 0 ? string.Empty : rest[assemblyEnd..];

            if (typeName.Length == 0 || assembly.Length == 0)
                return null;

            return new GenericArgument(SerializedTypeIdentity.FromFullName(typeName, assembly, '+'), tail);
        }

        private static int FirstTopLevelComma(string text)
        {
            var depth = 0;
            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '[': depth++; break;
                    case ']': depth--; break;
                    case ',' when depth == 0: return i;
                }
            }
            return -1;
        }
    }
}
