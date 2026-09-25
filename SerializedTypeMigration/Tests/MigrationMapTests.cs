using System;
using System.Linq;
using EDIVE.SerializedTypeMigration.Editor;
using NUnit.Framework;

namespace EDIVE.SerializedTypeMigration.Tests
{
    public class MigrationMapTests
    {
        private const string ASM = "EDIVE.SerializedTypeMigration.TestTypes";
        private const string NS = "EDIVE.SerializedTypeMigration.Tests";
        private const string LEGACY_NS = "EDIVE.SerializedTypeMigration.Tests.Legacy";
        private const string CURRENT_NS = "EDIVE.SerializedTypeMigration.Tests.Current";

        private static MigrationMap MapOf(params Type[] types) => MigrationMap.Build(types);

        private static SerializedTypeIdentity Id(string className, string ns = NS, string assembly = ASM) =>
            new(className, ns, assembly);

        private static string Errors(MigrationMap map) => string.Join("; ", map.Errors);

        [Test]
        public void SingleHop()
        {
            var map = MapOf(typeof(RenamedPayload));

            Assert.IsTrue(map.IsValid, Errors(map));
            Assert.AreEqual(Id("RenamedPayload"), map.Resolve(Id("RenamedPayloadLegacy")));
        }

        [Test]
        public void UnknownIdentity_IsLeftAlone()
        {
            var map = MapOf(typeof(RenamedPayload));
            var unrelated = Id("SomethingElse", "Other.Ns", "Other.Asm");

            Assert.AreEqual(unrelated, map.Resolve(unrelated));
        }

        [Test]
        public void EmptyIdentity_IsLeftAlone()
        {
            var empty = new SerializedTypeIdentity("", "", "");

            Assert.AreEqual(empty, MapOf(typeof(RenamedPayload)).Resolve(empty));
        }

        // rename hop keeps the namespace of its own time, not today's
        [Test]
        public void Chain_OlderHopKeepsItsOwnEra()
        {
            var map = MapOf(typeof(ChainedPayload));
            var current = Id("ChainedPayload");

            Assert.IsTrue(map.IsValid, Errors(map));
            Assert.AreEqual(3, map.Hops.Count);
            Assert.AreEqual(current, map.Resolve(Id("ChainV0", LEGACY_NS)), "version 0");
            Assert.AreEqual(current, map.Resolve(Id("ChainV1", LEGACY_NS)), "version 1");
            Assert.AreEqual(current, map.Resolve(Id("ChainV1", CURRENT_NS)), "version 2");
        }

        [Test]
        public void Chain_NeverInventsAnIdentityThatDidNotExist()
        {
            var map = MapOf(typeof(ChainedPayload));

            Assert.IsFalse(map.Hops.ContainsKey(Id("ChainV0")));
            Assert.IsFalse(map.Hops.ContainsKey(Id("ChainV0", CURRENT_NS)));
        }

        [Test]
        public void Chain_OrderComesFromVersionNotSourceOrder()
        {
            var declared = typeof(ChainedPayload)
                .GetCustomAttributes(typeof(FormerlySerializedTypeAttribute), false)
                .Cast<FormerlySerializedTypeAttribute>()
                .Select(attribute => attribute.Version)
                .ToArray();

            CollectionAssert.AreNotEqual(declared.OrderBy(version => version).ToArray(), declared,
                "ChainedPayload must declare hops out of order for this test to mean anything");
            Assert.AreEqual(Id("ChainedPayload"), MapOf(typeof(ChainedPayload)).Resolve(Id("ChainV0", LEGACY_NS)));
        }

        [Test]
        public void NestedType()
        {
            Assert.AreEqual(Id("NestingOwner/NestedPayload"),
                MapOf(typeof(NestingOwner.NestedPayload)).Resolve(Id("NestingOwner/NestedPayload", LEGACY_NS)));
        }

        [Test]
        public void EmptyNamespace_IsARealValue()
        {
            var map = MapOf(typeof(FormerlyGlobalPayload));

            Assert.IsTrue(map.IsValid, Errors(map));
            Assert.AreEqual(Id("FormerlyGlobalPayload"), map.Resolve(Id("GlobalLegacy", "")));
        }

        [Test]
        public void Generic_OpenTypeMoves()
        {
            var after = MapOf(typeof(BoxedPayload<>)).Resolve(Id("BoxedPayload`1[[System.Int32, mscorlib]]", LEGACY_NS));

            Assert.AreEqual(Id("BoxedPayload`1[[System.Int32, mscorlib]]"), after);
        }

        [Test]
        public void Generic_ArgumentMoves()
        {
            var after = MapOf(typeof(ArgumentPayload)).Resolve(Id($"BoxedPayload`1[[{NS}.ArgumentLegacy, {ASM}]]"));

            Assert.AreEqual($"BoxedPayload`1[[{NS}.ArgumentPayload, {ASM}]]", after.Class);
        }

        [Test]
        public void Generic_OpenAndArgumentMove()
        {
            var map = MapOf(typeof(BoxedPayload<>), typeof(ArgumentPayload));

            var after = map.Resolve(Id($"BoxedPayload`1[[{NS}.ArgumentLegacy, {ASM}]]", LEGACY_NS));

            Assert.AreEqual(Id($"BoxedPayload`1[[{NS}.ArgumentPayload, {ASM}]]"), after);
        }

        [Test]
        public void Generic_EnumArgumentMoves()
        {
            var after = MapOf(typeof(TestMode)).Resolve(Id($"BoxedPayload`1[[{NS}.LegacyMode, {ASM}]]"));

            Assert.AreEqual($"BoxedPayload`1[[{NS}.TestMode, {ASM}]]", after.Class);
        }

        [Test]
        public void Generic_OnlyTheMatchingArgumentOfSeveralMoves()
        {
            var after = MapOf(typeof(ArgumentPayload))
                .Resolve(Id($"Pair`2[[System.Int32, mscorlib],[{NS}.ArgumentLegacy, {ASM}]]"));

            Assert.AreEqual($"Pair`2[[System.Int32, mscorlib],[{NS}.ArgumentPayload, {ASM}]]", after.Class);
        }

        [Test]
        public void Generic_NestedGenericArgumentMoves()
        {
            var map = MapOf(typeof(BoxedPayload<>), typeof(ArgumentPayload));
            var before = Id($"Holder`1[[{LEGACY_NS}.BoxedPayload`1[[{NS}.ArgumentLegacy, {ASM}]], {ASM}]]", "Other.Ns", "Other.Asm");

            var after = map.Resolve(before);

            Assert.AreEqual($"Holder`1[[{NS}.BoxedPayload`1[[{NS}.ArgumentPayload, {ASM}]], {ASM}]]", after.Class);
            Assert.AreEqual("Other.Ns", after.Namespace);
        }

        [Test]
        public void Generic_NestedClassArgumentUsesPlus()
        {
            var after = MapOf(typeof(NestingOwner.NestedPayload))
                .Resolve(Id($"BoxedPayload`1[[{LEGACY_NS}.NestingOwner+NestedPayload, {ASM}]]"));

            Assert.AreEqual($"BoxedPayload`1[[{NS}.NestingOwner+NestedPayload, {ASM}]]", after.Class);
        }

        // names that only look like the old one, must stay
        [TestCase("Other." + NS + ".ArgumentLegacy, " + ASM)]
        [TestCase(NS + ".ArgumentLegacy, " + ASM + ".Editor")]
        [TestCase(NS + ".ArgumentLegacyExtra, " + ASM)]
        public void Generic_LookalikeArgumentsAreLeftAlone(string argument)
        {
            var before = Id($"BoxedPayload`1[[{argument}]]");

            Assert.AreEqual(before, MapOf(typeof(ArgumentPayload)).Resolve(before));
        }

        [Test]
        public void Generic_ArgumentTailIsKept()
        {
            var after = MapOf(typeof(ArgumentPayload))
                .Resolve(Id($"BoxedPayload`1[[{NS}.ArgumentLegacy, {ASM}, Version=1.0.0.0, Culture=neutral]]"));

            Assert.AreEqual($"BoxedPayload`1[[{NS}.ArgumentPayload, {ASM}, Version=1.0.0.0, Culture=neutral]]", after.Class);
        }

        [Test]
        public void Generic_MalformedIsLeftAlone()
        {
            var before = Id($"BoxedPayload`1[[{NS}.ArgumentLegacy, {ASM}]");

            Assert.AreEqual(before, MapOf(typeof(ArgumentPayload)).Resolve(before));
        }

        [Test]
        public void MovedFrom_IsImportedAsAHop()
        {
            var map = MapOf(typeof(MovedFromPayload));
            var old = Id("MovedFromLegacy", LEGACY_NS);

            Assert.IsTrue(map.IsValid, Errors(map));
            Assert.AreEqual(Id("MovedFromPayload"), map.Resolve(old));
            Assert.AreEqual(HopSource.MovedFrom, map.Sources[old]);
        }

        [Test]
        public void MovedFrom_LosesToFormerlySerializedType_WithWarning()
        {
            var map = MapOf(typeof(SharedLoser), typeof(SharedWinner));

            Assert.IsTrue(map.IsValid, Errors(map));
            Assert.AreEqual(Id("SharedWinner"), map.Resolve(Id("SharedLegacy")));
            Assert.IsNotEmpty(map.Warnings);
        }

        [Test]
        public void MovedFrom_NamingALiveTypeIsDroppedAndAnError()
        {
            var map = MapOf(typeof(MovedFromHijacker));

            Assert.IsFalse(map.IsValid);
            Assert.IsEmpty(map.Hops);
            Assert.IsTrue(map.Errors.Any(error => error.Contains("Remove the [MovedFrom]")), Errors(map));
        }

        [Test]
        public void UnityLoadsWithoutMigration_OnlyPlainMovedFrom()
        {
            var map = MapOf(typeof(MovedFromPayload), typeof(RenamedPayload));

            Assert.IsTrue(map.UnityLoadsWithoutMigration(Id("MovedFromLegacy", LEGACY_NS)));
            Assert.IsFalse(map.UnityLoadsWithoutMigration(Id("RenamedPayloadLegacy")), "[FormerlySerializedType] needs the migration");
            Assert.IsFalse(map.UnityLoadsWithoutMigration(Id("Box`1[[System.Int32, mscorlib]]", LEGACY_NS)), "Unity skips generics");
        }

        [Test]
        public void ValidTypes_HaveNoErrors()
        {
            var map = MapOf(typeof(RenamedPayload), typeof(ChainedPayload), typeof(BoxedPayload<>), typeof(TestMode),
                typeof(NestingOwner.NestedPayload), typeof(ArgumentPayload), typeof(FormerlyGlobalPayload), typeof(MovedFromPayload));

            CollectionAssert.IsEmpty(map.Errors);
            CollectionAssert.IsEmpty(map.Warnings);
        }

        [TestCase(typeof(InvalidDuplicateVersion), "more than once")]
        [TestCase(typeof(InvalidNoOp), "changes nothing")]
        [TestCase(typeof(InvalidEmptyClass), "cannot be empty")]
        [TestCase(typeof(InvalidHijacker), "still a live type")]
        public void Invalid(Type type, string expected)
        {
            var map = MapOf(type);

            Assert.IsFalse(map.IsValid);
            Assert.IsTrue(map.Errors.Any(error => error.Contains(expected)), Errors(map));
        }

        [Test]
        public void Invalid_DuplicateVersionContributesNothing()
        {
            CollectionAssert.IsEmpty(MapOf(typeof(InvalidDuplicateVersion)).Hops);
        }

        [Test]
        public void UnityObject_IsAWarningAndContributesNothing()
        {
            var map = MapOf(typeof(FormerlyOnAsset));

            Assert.IsTrue(map.IsValid, Errors(map));
            CollectionAssert.IsEmpty(map.Hops);
            Assert.IsTrue(map.Warnings.Any(warning => warning.Contains("UnityEngine.Object")), string.Join("; ", map.Warnings));
        }

        [Test]
        public void Invalid_TwoTypesClaimTheSameIdentity()
        {
            var map = MapOf(typeof(RenamedPayload), typeof(InvalidConflict));

            Assert.IsFalse(map.IsValid);
            Assert.IsTrue(map.Errors.Any(error => error.Contains("claimed by both")), Errors(map));
        }

        [Test]
        public void ProjectWideBuild_SkipsTestOnlyAssemblies()
        {
            var map = MigrationMap.Build();

            Assert.IsFalse(map.Hops.Values.Any(target => target.Assembly == ASM),
                "TestTypes asmdef is constrained to UNITY_INCLUDE_TESTS");
        }
    }
}
