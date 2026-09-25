using System.Linq;
using EDIVE.SerializedTypeMigration.Editor;
using NUnit.Framework;

namespace EDIVE.SerializedTypeMigration.Tests
{
    public class SerializedTypeYamlTests
    {
        private static SerializedTypeIdentity Id(string className, string ns = "Ns", string assembly = "Asm") =>
            new(className, ns, assembly);

        private static string Unchanged(string content)
        {
            var result = SerializedTypeYaml.Rewrite(content, id => id, out var rewritten);
            Assert.AreEqual(0, rewritten, "identity resolve must rewrite nothing");
            return result;
        }

        [Test]
        public void Read_PlainTriple()
        {
            const string yaml = "      type: {class: StablePayload, ns: EDIVE.Tests, asm: Some.Asm}";

            var id = SerializedTypeYaml.Read(yaml).Single();

            Assert.AreEqual(new SerializedTypeIdentity("StablePayload", "EDIVE.Tests", "Some.Asm"), id);
        }

        [Test]
        public void Read_NestedTriple()
        {
            const string yaml = "      type: {class: Outer/Inner, ns: EDIVE.Tests, asm: Some.Asm}";

            Assert.AreEqual("Outer/Inner", SerializedTypeYaml.Read(yaml).Single().Class);
        }

        [Test]
        public void Read_QuotedInflatedTriple()
        {
            const string yaml = "      type: {class: 'Boxed`1[[System.Int32, mscorlib]]', ns: EDIVE.Tests, asm: Some.Asm}";

            var id = SerializedTypeYaml.Read(yaml).Single();

            Assert.AreEqual("Boxed`1[[System.Int32, mscorlib]]", id.Class);
            Assert.AreEqual("EDIVE.Tests", id.Namespace);
        }

        [Test]
        public void Read_EmptyTripleIsSkipped()
        {
            const string yaml = "      type: {class: , ns: , asm: }";

            Assert.IsEmpty(SerializedTypeYaml.Read(yaml).ToList());
        }

        [Test]
        public void Read_PrefabOverrideValue()
        {
            const string yaml =
                "    - propertyPath: 'managedReferences[123]'\n" +
                "      value: Assembly-CSharp EDIVE.Tests.StablePayload\n";

            var id = SerializedTypeYaml.Read(yaml).Single();

            Assert.AreEqual(new SerializedTypeIdentity("StablePayload", "EDIVE.Tests", "Assembly-CSharp"), id);
        }

        [Test]
        public void Read_PrefabOverrideValue_QuotedGeneric()
        {
            const string yaml =
                "    - propertyPath: 'managedReferences[123]'\n" +
                "      value: 'Assembly-CSharp EDIVE.Tests.Boxed`1[[System.Int32, mscorlib]]'\n";

            Assert.AreEqual("Boxed`1[[System.Int32, mscorlib]]", SerializedTypeYaml.Read(yaml).Single().Class);
        }

        [Test]
        public void Read_IgnoresOverrideFieldValues()
        {
            const string yaml =
                "    - propertyPath: managedReferences[123].Label\n" +
                "      value: not a type\n" +
                "    - propertyPath: managedReferences[123].Value\n" +
                "      value: 42\n";

            Assert.IsEmpty(SerializedTypeYaml.Read(yaml).ToList(), "only the bare managedReferences[id] path carries a type");
        }

        [Test]
        public void Rewrite_IsByteIdenticalWhenNothingMoves()
        {
            const string yaml =
                "  references:\r\n" +
                "    version: 2\r\n" +
                "    RefIds:\r\n" +
                "    - rid: 123\r\n" +
                "      type: {class: StablePayload, ns: EDIVE.Tests, asm: Some.Asm}\r\n" +
                "      data:\r\n" +
                "        Note: hello\r\n" +
                "    - propertyPath: 'managedReferences[9]'\r\n" +
                "      value: Assembly-CSharp EDIVE.Tests.StablePayload\r\n";

            Assert.AreEqual(yaml, Unchanged(yaml));
        }

        [Test]
        public void Rewrite_LeavesUnrelatedMappingsAlone()
        {
            const string yaml =
                "  m_Script: {fileID: 11500000, guid: abc, type: 3}\n" +
                "  m_Colors: {r: 1, g: 0, b: 0, a: 1}\n" +
                "  m_Name: Probe\n";

            Assert.AreEqual(yaml, Unchanged(yaml));
        }

        [Test]
        public void Rewrite_Triple()
        {
            const string yaml = "      type: {class: Old, ns: Old.Ns, asm: Old.Asm}";

            var result = SerializedTypeYaml.Rewrite(yaml,
                id => id.Class == "Old" ? Id("New", "New.Ns", "New.Asm") : id, out var rewritten);

            Assert.AreEqual(1, rewritten);
            Assert.AreEqual("      type: {class: New, ns: New.Ns, asm: New.Asm}", result);
        }

        [Test]
        public void Rewrite_TripleAddsQuotesOnlyWhenNeeded()
        {
            const string yaml = "      type: {class: Old, ns: Old.Ns, asm: Old.Asm}";

            var result = SerializedTypeYaml.Rewrite(yaml,
                _ => Id("New`1[[System.Int32, mscorlib]]", "New.Ns", "New.Asm"), out _);

            Assert.AreEqual("      type: {class: 'New`1[[System.Int32, mscorlib]]', ns: New.Ns, asm: New.Asm}", result);
        }

        [Test]
        public void Rewrite_TripleDropsQuotesWhenNoLongerNeeded()
        {
            const string yaml = "      type: {class: 'Old`1[[System.Int32, mscorlib]]', ns: Old.Ns, asm: Old.Asm}";

            var result = SerializedTypeYaml.Rewrite(yaml, _ => Id("Plain", "New.Ns", "New.Asm"), out _);

            Assert.AreEqual("      type: {class: Plain, ns: New.Ns, asm: New.Asm}", result);
        }

        [Test]
        public void Rewrite_PrefabOverrideValue()
        {
            const string yaml =
                "    - propertyPath: 'managedReferences[123]'\n" +
                "      value: Old.Asm Old.Ns.Old\n" +
                "      objectReference: {fileID: 0}\n";

            var result = SerializedTypeYaml.Rewrite(yaml, _ => Id("New", "New.Ns", "New.Asm"), out var rewritten);

            Assert.AreEqual(1, rewritten);
            StringAssert.Contains("value: New.Asm New.Ns.New", result);
            StringAssert.Contains("objectReference: {fileID: 0}", result);
        }

        [Test]
        public void Rewrite_PrefabOverrideValue_QuotesGenerics()
        {
            const string yaml =
                "    - propertyPath: 'managedReferences[123]'\n" +
                "      value: Old.Asm Old.Ns.Old\n";

            var result = SerializedTypeYaml.Rewrite(yaml,
                _ => Id("New`1[[System.Int32, mscorlib]]", "New.Ns", "New.Asm"), out _);

            StringAssert.Contains("value: 'New.Asm New.Ns.New`1[[System.Int32, mscorlib]]'", result);
        }

        [Test]
        public void Rewrite_DoesNotTouchOverrideFieldValues()
        {
            const string yaml =
                "    - propertyPath: managedReferences[123].Label\n" +
                "      value: Old.Asm Old.Ns.Old\n";

            var result = SerializedTypeYaml.Rewrite(yaml, _ => Id("New", "New.Ns", "New.Asm"), out var rewritten);

            Assert.AreEqual(0, rewritten);
            Assert.AreEqual(yaml, result);
        }

        [Test]
        public void Read_OverrideSetToNullIsSkipped()
        {
            const string yaml =
                "    - propertyPath: 'managedReferences[123]'\n" +
                "      value: \n" +
                "      objectReference: {fileID: 0}\n";

            Assert.IsEmpty(SerializedTypeYaml.Read(yaml).ToList());
            Assert.AreEqual(yaml, Unchanged(yaml));
        }

        [Test]
        public void IsUnityYaml()
        {
            Assert.IsTrue(SerializedTypeYaml.IsUnityYaml("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"));
            Assert.IsFalse(SerializedTypeYaml.IsUnityYaml("\u0000\u0000\u0000type: {class: A, ns: B, asm: C}"));
        }

        [Test]
        public void Decode_KeepsTrackOfTheBom()
        {
            var withBom = new byte[] { 0xEF, 0xBB, 0xBF, (byte) '%' };

            Assert.AreEqual("%", SerializedTypeYaml.Decode(withBom, out var hasBom));
            Assert.IsTrue(hasBom);
            Assert.AreEqual("%", SerializedTypeYaml.Decode(new[] { (byte) '%' }, out hasBom));
            Assert.IsFalse(hasBom);
        }

        [TestCase("Prefabs/Thing.prefab", true)]
        [TestCase("Data/Thing.asset", true)]
        [TestCase("Textures/Thing.png", false)]
        [TestCase("Hidden~/Thing.prefab", false)]
        [TestCase(".git/Thing.prefab", false)]
        public void IsCandidate(string relativePath, bool expected)
        {
            Assert.AreEqual(expected, MigrationScanner.IsCandidate(relativePath));
        }

        [Test]
        public void MightContainReferences_CatchesBothEncodings()
        {
            Assert.IsTrue(SerializedTypeYaml.MightContainReferences("      type: {class: A, ns: B, asm: C}"));
            Assert.IsTrue(SerializedTypeYaml.MightContainReferences("propertyPath: 'managedReferences[1]'"),
                "a prefab variant can contain no asm: line at all");
            Assert.IsFalse(SerializedTypeYaml.MightContainReferences("m_Name: nothing to see"));
        }
    }
}
