using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using CompilationPipeline = UnityEditor.Compilation.CompilationPipeline;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace EDIVE.SerializedTypeMigration.Editor
{
    public enum HopSource
    {
        FormerlySerializedType,
        MovedFrom
    }

    // old name -> today's type. version chain is resolved once at build, so v0 and v1 both map straight to today
    public sealed class MigrationMap
    {
        private readonly Dictionary<SerializedTypeIdentity, SerializedTypeIdentity> _hops = new();
        private readonly Dictionary<SerializedTypeIdentity, HopSource> _sources = new();
        private readonly HashSet<SerializedTypeIdentity> _ambiguous = new();
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public IReadOnlyDictionary<SerializedTypeIdentity, SerializedTypeIdentity> Hops => _hops;
        public IReadOnlyDictionary<SerializedTypeIdentity, HopSource> Sources => _sources;
        public IReadOnlyList<string> Errors => _errors;     // block apply
        public IReadOnlyList<string> Warnings => _warnings; // do not block
        public LiveTypeIndex LiveTypes { get; } = new();
        public bool IsValid => _errors.Count == 0;

        public static MigrationMap Build()
        {
            var owned = new Dictionary<Assembly, bool>();
            var formerly = TypeCache.GetTypesWithAttribute<FormerlySerializedTypeAttribute>();
            var movedFrom = TypeCache.GetTypesWithAttribute<MovedFromAttribute>().Where(type => IsOwned(type) && !IsTestOnly(type));

            return Build(formerly.Concat(movedFrom).Distinct().Where(type => !IsTestOnly(type)));

            bool IsOwned(Type type) => owned.TryGetValue(type.Assembly, out var value)
                ? value
                : owned[type.Assembly] = IsProjectCode(type.Assembly);
        }

        public static MigrationMap Build(IEnumerable<Type> types)
        {
            var map = new MigrationMap();
            var list = types.ToList();

            // [FormerlySerializedType] first so it wins over [MovedFrom]
            list.ForEach(map.AddFormerlySerialized);
            list.ForEach(map.AddMovedFrom);

            map.DropHijacks();
            return map;
        }

        public SerializedTypeIdentity Resolve(SerializedTypeIdentity id)
        {
            if (id.IsEmpty)
                return id;

            var name = GenericTypeName.Parse(id.Class);
            if (name == null)
                return _hops.GetValueOrDefault(id, id);

            // generic: move "Boxed`1" and each argument separately, then put back together
            var open = new SerializedTypeIdentity(name.Open, id.Namespace, id.Assembly);
            open = _hops.GetValueOrDefault(open, open);

            if (name.Arguments.Count == 0)
                return open;

            var arguments = name.Arguments.Select(argument => argument.With(Resolve(argument.Identity)));
            return new SerializedTypeIdentity(GenericTypeName.Compose(open.Class, arguments), open.Namespace, open.Assembly);
        }

        // Unity still loads plain [MovedFrom] hops by itself. anything else is broken until migrated
        public bool UnityLoadsWithoutMigration(SerializedTypeIdentity id) =>
            !id.IsInflated && _sources.TryGetValue(id, out var source) && source == HopSource.MovedFrom;

        // UNITY_INCLUDE_TESTS asmdef. its old names are made up for tests, no real asset has them
        private static bool IsTestOnly(Type type)
        {
            var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(type.Assembly.GetName().Name);
            if (string.IsNullOrEmpty(asmdef))
                return false;

            var constraints = JsonUtility.FromJson<AsmdefConstraints>(File.ReadAllText(FileUtil.GetPhysicalPath(asmdef))).defineConstraints;
            return constraints != null && constraints.Contains("UNITY_INCLUDE_TESTS");
        }

        [Serializable]
        private class AsmdefConstraints
        {
            // ReSharper disable once InconsistentNaming - json key
            public string[] defineConstraints;
        }

        // only migrate [MovedFrom] of code we own, not registry packages
        private static bool IsProjectCode(Assembly assembly)
        {
            var name = assembly.GetName().Name;
            if (name.StartsWith("Assembly-CSharp", StringComparison.Ordinal))
                return true;

            var asmdef = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(name);
            if (string.IsNullOrEmpty(asmdef))
                return false;

            return asmdef.StartsWith("Assets/", StringComparison.Ordinal) ||
                   PackageInfo.FindForAssetPath(asmdef) is { source: PackageSource.Embedded or PackageSource.Local };
        }

        private void AddFormerlySerialized(Type type)
        {
            var attributes = type.GetCustomAttributes<FormerlySerializedTypeAttribute>(false).OrderBy(attribute => attribute.Version).ToList();
            if (attributes.Count == 0)
                return;

            var current = SerializedTypeIdentity.FromType(type);

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                _warnings.Add($"{current}: no effect on UnityEngine.Object, Unity finds it by script GUID. Remove it.");
                return;
            }

            var duplicate = attributes.GroupBy(attribute => attribute.Version).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                _errors.Add($"{current}: version {duplicate.Key} used more than once.");
                return;
            }

            if (attributes.Any(attribute => attribute.ClassName == string.Empty))
            {
                _errors.Add($"{current}: class name cannot be empty, use null to keep it.");
                return;
            }

            // walk newest to oldest. null part = copy from the version above, the newest copies today
            var newer = current;
            foreach (var attribute in Enumerable.Reverse(attributes))
            {
                var old = newer.Override(attribute.ClassName, attribute.Namespace, attribute.Assembly);
                if (old == newer)
                {
                    _errors.Add($"{current}: version {attribute.Version} changes nothing.");
                    continue;
                }

                AddHop(old, current, HopSource.FormerlySerializedType);
                newer = old;
            }
        }

        // [MovedFrom] null part = today's value, no chaining. Unity's rule
        private void AddMovedFrom(Type type)
        {
            var current = SerializedTypeIdentity.FromType(type);
            foreach (var data in type.GetCustomAttributesData().Where(data => data.AttributeType == typeof(MovedFromAttribute)))
            {
                var (className, ns, assembly) = ReadMovedFrom(data);
                var old = current.Override(className, ns, assembly);
                if (old != current)
                    AddHop(old, current, HopSource.MovedFrom);
            }
        }

        // ctor args: (bool autoUpdateAPI, ns, asm, class) or (ns)
        private static (string className, string ns, string assembly) ReadMovedFrom(CustomAttributeData data)
        {
            var args = data.ConstructorArguments.Select(argument => argument.Value).SkipWhile(value => value is bool).ToList();
            return (At(2), At(0), At(1));

            string At(int index) => index < args.Count ? args[index] as string : null;
        }

        private void AddHop(SerializedTypeIdentity old, SerializedTypeIdentity current, HopSource source)
        {
            if (_ambiguous.Contains(old))
                return;

            if (!_hops.TryGetValue(old, out var existing))
            {
                _hops[old] = current;
                _sources[old] = source;
                return;
            }

            if (existing == current)
                return;

            switch (_sources[old], source)
            {
                case (HopSource.FormerlySerializedType, HopSource.FormerlySerializedType):
                    _errors.Add($"'{old}' claimed by both {existing} and {current}.");
                    break;

                case (HopSource.FormerlySerializedType, HopSource.MovedFrom):
                    _warnings.Add($"'{old}': [MovedFrom] on {current} ignored, [FormerlySerializedType] on {existing} wins.");
                    break;

                default:
                    _warnings.Add($"'{old}': [MovedFrom] on both {existing} and {current}, skipped.");
                    _hops.Remove(old);
                    _sources.Remove(old);
                    _ambiguous.Add(old);
                    break;
            }
        }

        // old name is still a real type: migrating would turn its data into the other type
        private void DropHijacks()
        {
            foreach (var old in _hops.Keys.Where(LiveTypes.Contains).ToList())
            {
                if (_sources[old] == HopSource.FormerlySerializedType)
                {
                    _errors.Add($"'{old}' is still a live type but {_hops[old]} claims it.");
                    continue;
                }

                // skipping does not help. Unity applies [MovedFrom] itself and saves the real type's data as the other type
                _errors.Add($"'{old}' is still a live type but [MovedFrom] on {_hops[old]} claims it. " +
                            "Unity saves its instances as the other type. Remove the [MovedFrom].");
                _hops.Remove(old);
                _sources.Remove(old);
            }
        }
    }

    // does a serialized identity still match a loaded type, generic arguments included
    public sealed class LiveTypeIndex
    {
        // Unity writes mscorlib, CoreCLR has System.Private.CoreLib
        private const string MSCORLIB = "mscorlib";

        private readonly Dictionary<string, Assembly> _assemblies = new();

        public LiveTypeIndex()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                _assemblies[assembly.GetName().Name] = assembly;

            _assemblies.TryAdd(MSCORLIB, typeof(object).Assembly);
        }

        public bool Contains(SerializedTypeIdentity id)
        {
            if (id.IsEmpty)
                return true;

            if (!_assemblies.TryGetValue(id.Assembly, out var assembly) || assembly.GetType(id.ToReflectionName(), false) == null)
                return false;

            var name = GenericTypeName.Parse(id.Class);
            return name == null || name.Arguments.All(argument => Contains(argument.Identity));
        }
    }
}
