using System;

namespace EDIVE.SerializedTypeMigration
{
    // Old name of a [SerializeReference] type, or of a type used as its generic argument.
    // One per move. Version: lower is older, unique per type. Never delete old ones.
    // Null part = same as the next higher version, highest = same as today. "" is a real value (no namespace).
    // Nested class: "Outer/Inner". Assets are rewritten by Tools/Serialized Type Migration, not at load.
    //
    //   [FormerlySerializedType(0, "Foo")]          // Old.Ns.Foo
    //   [FormerlySerializedType(1, ns: "Old.Ns")]   // Old.Ns.Bar
    //   public class Bar { }                        // New.Ns.Bar
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public sealed class FormerlySerializedTypeAttribute : Attribute
    {
        public int Version { get; }
        public string ClassName { get; }
        public string Namespace { get; }
        public string Assembly { get; }

        public FormerlySerializedTypeAttribute(int version, string className = null, string ns = null, string assembly = null)
        {
            Version = version;
            ClassName = className;
            Namespace = ns;
            Assembly = assembly;
        }
    }
}
