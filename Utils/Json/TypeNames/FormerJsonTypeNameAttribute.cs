// Author: František Holubec
// Created: 15.06.2025

using System;

namespace EDIVE.Utils.Json.TypeNames
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class FormerJsonTypeNameAttribute : Attribute
    {
        public string TypeName { get; }

        public FormerJsonTypeNameAttribute(string typeName)
        {
            TypeName = typeName;
        }

        protected bool Equals(FormerJsonTypeNameAttribute other)
        {
            return base.Equals(other) && TypeName == other.TypeName;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((FormerJsonTypeNameAttribute) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), TypeName);
        }
    }
}
