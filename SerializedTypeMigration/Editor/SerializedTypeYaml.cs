using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EDIVE.SerializedTypeMigration.Editor
{
    // Unity writes a managed reference type in two single line forms:
    //   owned:    type: {class: X, ns: Y, asm: Z}
    //   override: propertyPath: managedReferences[id]  +  value: <asm> <full name>
    public static class SerializedTypeYaml
    {
        private const string YAML_HEADER = "%YAML";
        private static readonly string[] FILE_MARKERS = { "asm:", "managedReferences[" };

        private static readonly Regex TYPE_ENTRY = new(
            @"type:\s*\{\s*class:\s*(?<class>'(?:[^']|'')*'|[^,}]*?)\s*,\s*ns:\s*(?<ns>[^,}]*?)\s*,\s*asm:\s*(?<asm>[^,}]*?)\s*\}",
            RegexOptions.Compiled);

        // only bare managedReferences[id] is a type, managedReferences[id].Field is data
        private static readonly Regex OVERRIDE_ENTRY = new(
            @"(?<prefix>propertyPath:\s*'?managedReferences\[-?\d+\]'?\s*\r?\n\s*value:[ \t]*)(?<value>'(?:[^']|'')*'|[^\r\n]*)",
            RegexOptions.Compiled);

        private static readonly char[] QUOTE_TRIGGERS = { ',', '{', '}', '[', ']', '\'', '"', '#', ':', '&', '*', '!', '|', '>', '%', '@', '`' };

        private static readonly UTF8Encoding WITHOUT_BOM = new(false);
        private static readonly UTF8Encoding WITH_BOM = new(true);

        public static bool MightContainReferences(string content) => FILE_MARKERS.Any(content.Contains);

        // anything without the header is binary and never rewritten
        public static bool IsUnityYaml(string content) => content.StartsWith(YAML_HEADER, StringComparison.Ordinal);

        public static IEnumerable<SerializedTypeIdentity> Read(string content) =>
            TYPE_ENTRY.Matches(content).Select(FromTypeEntry)
                .Concat(OVERRIDE_ENTRY.Matches(content).Select(FromOverrideEntry))
                .Where(id => !id.IsEmpty);

        // unchanged entries stay byte identical
        public static string Rewrite(string content, Func<SerializedTypeIdentity, SerializedTypeIdentity> resolve, out int rewritten)
        {
            var count = 0;

            var result = TYPE_ENTRY.Replace(content, match =>
                Replace(match, FromTypeEntry(match), Write));

            result = OVERRIDE_ENTRY.Replace(result, match =>
                Replace(match, FromOverrideEntry(match), resolved => match.Groups["prefix"].Value + Quote(resolved.ToOverrideValue())));

            rewritten = count;
            return result;

            string Replace(Match match, SerializedTypeIdentity id, Func<SerializedTypeIdentity, string> write)
            {
                var resolved = id.IsEmpty ? id : resolve(id);
                if (resolved == id)
                    return match.Value;

                count++;
                return write(resolved);
            }
        }

        public static string Write(SerializedTypeIdentity id) =>
            $"type: {{class: {Quote(id.Class)}, ns: {id.Namespace}, asm: {id.Assembly}}}";

        // keep the BOM if the file had one
        public static string Decode(byte[] bytes, out bool hasBom)
        {
            hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var offset = hasBom ? 3 : 0;
            return WITHOUT_BOM.GetString(bytes, offset, bytes.Length - offset);
        }

        public static void WriteFile(string path, string content, bool hasBom) =>
            File.WriteAllText(path, content, hasBom ? WITH_BOM : WITHOUT_BOM);

        private static SerializedTypeIdentity FromTypeEntry(Match match) =>
            new(Unquote(match.Groups["class"].Value), match.Groups["ns"].Value, match.Groups["asm"].Value);

        private static SerializedTypeIdentity FromOverrideEntry(Match match) =>
            SerializedTypeIdentity.FromOverrideValue(Unquote(match.Groups["value"].Value));

        private static string Unquote(string value) =>
            value.Length >= 2 && value[0] == '\'' && value[^1] == '\'' ? value[1..^1].Replace("''", "'") : value;

        private static string Quote(string value) =>
            string.IsNullOrEmpty(value) || value.IndexOfAny(QUOTE_TRIGGERS) < 0 ? value : $"'{value.Replace("'", "''")}'";
    }
}
