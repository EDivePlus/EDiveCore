using System.Linq;
using EDIVE.SerializedTypeMigration.Editor;
using NUnit.Framework;

namespace EDIVE.SerializedTypeMigration.Tests
{
    public class SerializedTypeIdentityTests
    {
        private const string ASM = "EDIVE.SerializedTypeMigration.TestTypes";
        private const string NS = "EDIVE.SerializedTypeMigration.Tests";

        [Test]
        public void FromType_PlainClass()
        {
            var id = SerializedTypeIdentity.FromType(typeof(StablePayload));

            Assert.AreEqual("StablePayload", id.Class);
            Assert.AreEqual(NS, id.Namespace);
            Assert.AreEqual(ASM, id.Assembly);
        }

        [Test]
        public void FromType_NestedClass_UsesSlash()
        {
            var id = SerializedTypeIdentity.FromType(typeof(NestingOwner.NestedPayload));

            Assert.AreEqual("NestingOwner/NestedPayload", id.Class);
            Assert.AreEqual(NS, id.Namespace, "nested types keep the outer namespace");
        }

        [Test]
        public void FromType_GenericDefinition_KeepsArity()
        {
            var id = SerializedTypeIdentity.FromType(typeof(BoxedPayload<>));

            Assert.AreEqual("BoxedPayload`1", id.Class);
        }

        [Test]
        public void Open_StripsGenericArguments()
        {
            var id = new SerializedTypeIdentity("BoxedPayload`1[[System.Int32, mscorlib]]", NS, ASM);

            Assert.IsTrue(id.IsInflated);
            Assert.AreEqual("BoxedPayload`1", id.Open().Class);
            Assert.AreEqual("[[System.Int32, mscorlib]]", id.GenericArguments);
        }

        [Test]
        public void Open_OnPlainIdentity_IsUnchanged()
        {
            var id = new SerializedTypeIdentity("StablePayload", NS, ASM);

            Assert.IsFalse(id.IsInflated);
            Assert.AreEqual(id, id.Open());
            Assert.AreEqual(string.Empty, id.GenericArguments);
        }

        [Test]
        public void Override_NullPartsAreKept()
        {
            var id = new SerializedTypeIdentity("Bar", "New.Ns", "New.Asm");

            Assert.AreEqual(new SerializedTypeIdentity("Foo", "New.Ns", "New.Asm"), id.Override("Foo", null, null));
            Assert.AreEqual(new SerializedTypeIdentity("Bar", "Old.Ns", "New.Asm"), id.Override(null, "Old.Ns", null));
            Assert.AreEqual(new SerializedTypeIdentity("Bar", "New.Ns", "Old.Asm"), id.Override(null, null, "Old.Asm"));
            Assert.AreEqual(id, id.Override(null, null, null));
        }

        [TestCase("StablePayload", "EDIVE.Tests", "Some.Asm")]
        [TestCase("Outer/Inner", "EDIVE.Tests", "Some.Asm")]
        [TestCase("BoxedPayload`1[[System.Int32, mscorlib]]", "EDIVE.Tests", "Some.Asm")]
        [TestCase("NoNamespace", "", "Some.Asm")]
        public void OverrideValue_RoundTrips(string className, string ns, string assembly)
        {
            var id = new SerializedTypeIdentity(className, ns, assembly);

            Assert.AreEqual(id, SerializedTypeIdentity.FromOverrideValue(id.ToOverrideValue()));
        }

        [Test]
        public void OverrideValue_MatchesUnityFormat()
        {
            var id = new SerializedTypeIdentity("Outer/Inner", "EDIVE.Tests", "Assembly-CSharp");

            Assert.AreEqual("Assembly-CSharp EDIVE.Tests.Outer/Inner", id.ToOverrideValue());
        }

        [Test]
        public void ToReflectionName_UsesPlusForNesting()
        {
            var id = SerializedTypeIdentity.FromType(typeof(NestingOwner.NestedPayload));

            Assert.AreEqual($"{NS}.NestingOwner+NestedPayload", id.ToReflectionName());
        }

        [Test]
        public void EqualityAndHashing()
        {
            var a = new SerializedTypeIdentity("Foo", "Ns", "Asm");
            var b = new SerializedTypeIdentity("Foo", "Ns", "Asm");
            var c = new SerializedTypeIdentity("Foo", "Ns", "Other");

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreEqual(1, new[] { a, b }.Distinct().Count());
        }

        [Test]
        public void LiveTypeIndex_KnowsWhatExists()
        {
            var index = new LiveTypeIndex();

            Assert.IsTrue(index.Contains(SerializedTypeIdentity.FromType(typeof(StablePayload))));
            Assert.IsTrue(index.Contains(SerializedTypeIdentity.FromType(typeof(NestingOwner.NestedPayload))));
            Assert.IsFalse(index.Contains(new SerializedTypeIdentity("GoneForever", NS, ASM)));
            Assert.IsFalse(index.Contains(new SerializedTypeIdentity("StablePayload", NS, "No.Such.Assembly")));
        }

        [Test]
        public void LiveTypeIndex_ChecksGenericArguments()
        {
            var index = new LiveTypeIndex();

            Assert.IsTrue(index.Contains(new SerializedTypeIdentity("BoxedPayload`1[[System.Int32, mscorlib]]", NS, ASM)),
                "mscorlib is how Unity writes core types");
            Assert.IsTrue(index.Contains(new SerializedTypeIdentity($"BoxedPayload`1[[{NS}.NestingOwner+NestedPayload, {ASM}]]", NS, ASM)));
            Assert.IsFalse(index.Contains(new SerializedTypeIdentity($"BoxedPayload`1[[{NS}.GoneForever, {ASM}]]", NS, ASM)),
                "live outer type with a dead argument is still broken");
        }
    }
}
