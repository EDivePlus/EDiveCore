using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EDIVE.SerializedTypeMigration.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EDIVE.SerializedTypeMigration.Tests
{
    // build with today's types, rename back in the text, migrate, check the data
    public class MigrationEndToEndTests
    {
        private const string FOLDER = "Assets/__MigrationTests__";
        private const string ASM = "EDIVE.SerializedTypeMigration.TestTypes";
        private const string NS = "EDIVE.SerializedTypeMigration.Tests";

        private bool _scenesTouched;

        private static string Absolute(string assetPath) => Path.GetFullPath(assetPath);

        [SetUp]
        public void CreateFolder()
        {
            if (!AssetDatabase.IsValidFolder(FOLDER))
                AssetDatabase.CreateFolder("Assets", "__MigrationTests__");
        }

        // broken imports log errors on purpose. must be set in the body, SetUp does not reach it
        private static void ExpectBrokenImportErrors() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void DeleteFolder()
        {
            try
            {
                if (_scenesTouched)
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                if (AssetDatabase.IsValidFolder(FOLDER))
                    AssetDatabase.DeleteAsset(FOLDER);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        // never replace scenes over unsaved work
        private void TakeOverScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                    Assert.Inconclusive("An open scene has unsaved changes. Save or discard it, then rerun.");
            }
            _scenesTouched = true;
        }

        private static void SendBackInTime(string assetPath, string currentClass, string legacyClass)
        {
            var absolute = Absolute(assetPath);
            var before = File.ReadAllText(absolute);
            var after = before
                .Replace("class: " + currentClass + ",", "class: " + legacyClass + ",")
                .Replace("value: " + ASM + " " + NS + "." + currentClass + "\n", "value: " + ASM + " " + NS + "." + legacyClass + "\n")
                .Replace("value: " + ASM + " " + NS + "." + currentClass + "\r\n", "value: " + ASM + " " + NS + "." + legacyClass + "\r\n");

            Assert.AreNotEqual(before, after, "nothing was sent back in time in " + assetPath);
            File.WriteAllText(absolute, after);
        }

        private static MigrationMap TestTypesMap()
        {
            var map = MigrationMap.Build(new[]
            {
                typeof(RenamedPayload), typeof(ChainedPayload), typeof(NestingOwner.NestedPayload),
                typeof(BoxedPayload<>), typeof(ArgumentPayload)
            });
            Assert.IsTrue(map.IsValid, string.Join("; ", map.Errors));
            return map;
        }

        private static void Migrate(params string[] assetPaths)
        {
            var changed = MigrationScanner.Apply(TestTypesMap(), assetPaths.Select(Absolute).ToList());
            Assert.IsNotEmpty(changed, "migration rewrote nothing");
        }

        private static void CreateHost(string assetPath)
        {
            var host = ScriptableObject.CreateInstance<TestAsset>();
            host.Single = new RenamedPayload { Value = 42, Label = "single" };
            host.Many.Add(new RenamedPayload { Value = 7, Label = "list-a" });
            host.Many.Add(new StablePayload { Note = "stable" });

            AssetDatabase.CreateAsset(host, assetPath);
            EditorUtility.SetDirty(host);
            AssetDatabase.SaveAssets();
        }

        private static void AssertHostRecovered(string assetPath)
        {
            var host = AssetDatabase.LoadAssetAtPath<TestAsset>(assetPath);
            Assert.IsFalse(SerializationUtility.HasManagedReferencesWithMissingTypes(host));

            var single = host.Single as RenamedPayload;
            Assert.IsNotNull(single);
            Assert.AreEqual(42, single.Value);
            Assert.AreEqual("single", single.Label);

            var first = host.Many[0] as RenamedPayload;
            Assert.IsNotNull(first);
            Assert.AreEqual("list-a", first.Label);

            var stable = host.Many[1] as StablePayload;
            Assert.IsNotNull(stable, "a type that never moved must be untouched");
            Assert.AreEqual("stable", stable.Note);
        }

        [Test]
        public void ScriptableObject_Recovers()
        {
            const string path = FOLDER + "/Host.asset";
            CreateHost(path);
            SendBackInTime(path, "RenamedPayload", "RenamedPayloadLegacy");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Assert.IsTrue(SerializationUtility.HasManagedReferencesWithMissingTypes(AssetDatabase.LoadAssetAtPath<TestAsset>(path)),
                "asset should be broken before migrating");

            Migrate(path);
            AssertHostRecovered(path);
        }

        // Unity keeps missing data on save
        [Test]
        public void ScriptableObject_SurvivesSaveWhileBroken()
        {
            const string path = FOLDER + "/Host.asset";
            CreateHost(path);
            SendBackInTime(path, "RenamedPayload", "RenamedPayloadLegacy");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var broken = AssetDatabase.LoadAssetAtPath<TestAsset>(path);
            EditorUtility.SetDirty(broken);
            AssetDatabase.SaveAssets();
            StringAssert.Contains("RenamedPayloadLegacy", File.ReadAllText(Absolute(path)));

            Migrate(path);
            AssertHostRecovered(path);
        }

        [Test]
        public void ScriptableObject_UntouchedAssetIsNotWritten()
        {
            const string path = FOLDER + "/Host.asset";
            CreateHost(path);
            var before = File.ReadAllBytes(Absolute(path));

            var changed = MigrationScanner.Apply(TestTypesMap(), new List<string> { Absolute(path) });

            CollectionAssert.IsEmpty(changed);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(Absolute(path)), "file must stay byte identical");
        }

        [Test]
        public void ScriptableObject_RecoversAcrossThreeHops()
        {
            const string path = FOLDER + "/Chain.asset";
            var host = ScriptableObject.CreateInstance<TestAsset>();
            host.Single = new ChainedPayload { Value = 5 };
            AssetDatabase.CreateAsset(host, path);
            EditorUtility.SetDirty(host);
            AssetDatabase.SaveAssets();

            var absolute = Absolute(path);
            File.WriteAllText(absolute, File.ReadAllText(absolute)
                .Replace("class: ChainedPayload, ns: " + NS + ",", "class: ChainV0, ns: " + NS + ".Legacy,"));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Migrate(path);

            var recovered = AssetDatabase.LoadAssetAtPath<TestAsset>(path).Single as ChainedPayload;
            Assert.IsNotNull(recovered, "the oldest hop of a three hop chain must still resolve");
            Assert.AreEqual(5, recovered.Value);
        }

        [Test]
        public void ScriptableObject_GenericWithMovedArgument()
        {
            const string path = FOLDER + "/Boxed.asset";
            var host = ScriptableObject.CreateInstance<TestAsset>();
            host.Single = new BoxedPayload<ArgumentPayload> { Item = new ArgumentPayload { Number = 9 } };
            AssetDatabase.CreateAsset(host, path);
            EditorUtility.SetDirty(host);
            AssetDatabase.SaveAssets();

            var absolute = Absolute(path);
            var before = File.ReadAllText(absolute);
            var after = before.Replace(NS + ".ArgumentPayload, " + ASM, NS + ".ArgumentLegacy, " + ASM);
            Assert.AreNotEqual(before, after, "Unity did not write the argument where expected");
            File.WriteAllText(absolute, after);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Migrate(path);

            var recovered = AssetDatabase.LoadAssetAtPath<TestAsset>(path).Single as BoxedPayload<ArgumentPayload>;
            Assert.IsNotNull(recovered);
            Assert.AreEqual(9, recovered.Item.Number);
        }

        [Test]
        public void Prefab_Recovers()
        {
            const string path = FOLDER + "/Base.prefab";
            CreatePrefab(path);
            SendBackInTime(path, "RenamedPayload", "RenamedPayloadLegacy");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            Migrate(path);

            var single = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<TestBehaviour>().Single as RenamedPayload;
            Assert.IsNotNull(single);
            Assert.AreEqual("base-single", single.Label);
        }

        [Test]
        public void PrefabVariant_RecoversOverrides()
        {
            const string basePath = FOLDER + "/Base.prefab";
            const string variantPath = FOLDER + "/Variant.prefab";
            CreateVariant(CreatePrefab(basePath), variantPath);

            Assert.IsFalse(File.ReadAllText(Absolute(variantPath)).Contains("asm:"),
                "a variant stores types only in the override encoding");

            SendBackInTime(basePath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(variantPath, "RenamedPayload", "RenamedPayloadLegacy");

            Migrate(basePath, variantPath);
            AssertVariantRecovered(variantPath);
        }

        [Test]
        public void PrefabVariant_SurvivesSaveWhileBroken()
        {
            const string basePath = FOLDER + "/Base.prefab";
            const string variantPath = FOLDER + "/Variant.prefab";
            CreateVariant(CreatePrefab(basePath), variantPath);
            SendBackInTime(basePath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(variantPath, "RenamedPayload", "RenamedPayloadLegacy");
            ExpectBrokenImportErrors();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var root = PrefabUtility.LoadPrefabContents(variantPath);
            root.transform.position = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, variantPath);
            PrefabUtility.UnloadPrefabContents(root);
            StringAssert.Contains("RenamedPayloadLegacy", File.ReadAllText(Absolute(variantPath)));

            Migrate(basePath, variantPath);
            AssertVariantRecovered(variantPath);
        }

        [Test]
        public void SceneInstance_RecoversOverridesOnTopOfAVariant()
        {
            TakeOverScenes();
            const string basePath = FOLDER + "/Base.prefab";
            const string variantPath = FOLDER + "/Variant.prefab";
            const string scenePath = FOLDER + "/Probe.unity";
            CreateScene(CreateVariant(CreatePrefab(basePath), variantPath), scenePath);

            SendBackInTime(basePath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(variantPath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(scenePath, "RenamedPayload", "RenamedPayloadLegacy");

            Migrate(basePath, variantPath, scenePath);
            AssertSceneRecovered(scenePath);
        }

        // Unity logs broken merges a frame later
        [UnityTest]
        public IEnumerator SceneInstance_SurvivesSaveWhileBroken()
        {
            TakeOverScenes();
            const string basePath = FOLDER + "/Base.prefab";
            const string variantPath = FOLDER + "/Variant.prefab";
            const string scenePath = FOLDER + "/Probe.unity";
            CreateScene(CreateVariant(CreatePrefab(basePath), variantPath), scenePath);
            SendBackInTime(basePath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(variantPath, "RenamedPayload", "RenamedPayloadLegacy");
            SendBackInTime(scenePath, "RenamedPayload", "RenamedPayloadLegacy");
            ExpectBrokenImportErrors();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            yield return null;
            _ = new GameObject("Unrelated");
            EditorSceneManager.SaveScene(scene);
            StringAssert.Contains("RenamedPayloadLegacy", File.ReadAllText(Absolute(scenePath)));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return null;
            yield return null;

            Migrate(basePath, variantPath, scenePath);
            AssertSceneRecovered(scenePath);
        }

        [Test]
        public void BinaryFile_IsReportedAndNeverWritten()
        {
            var path = Path.Combine(Path.GetTempPath(), "SerializedTypeMigrationBinary.asset");
            var bytes = new byte[] { 0, 1, 2, 3 }
                .Concat(System.Text.Encoding.UTF8.GetBytes($"type: {{class: RenamedPayloadLegacy, ns: {NS}, asm: {ASM}}}"))
                .Concat(new byte[] { 0, 0xFF })
                .ToArray();
            File.WriteAllBytes(path, bytes);
            try
            {
                var map = TestTypesMap();
                var files = new List<string> { path };

                CollectionAssert.IsEmpty(MigrationScanner.Apply(map, files));
                CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
                CollectionAssert.Contains(MigrationScanner.Scan(map, files).BinaryWithOldTypes, path);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void FindPending_OnlyListsFilesWithOldNames()
        {
            const string broken = FOLDER + "/Broken.asset";
            const string healthy = FOLDER + "/Healthy.asset";
            CreateHost(broken);
            CreateHost(healthy);
            SendBackInTime(broken, "RenamedPayload", "RenamedPayloadLegacy");

            var pending = MigrationScanner.FindPending(TestTypesMap(), new[] { Absolute(broken), Absolute(healthy) });

            var old = new SerializedTypeIdentity("RenamedPayloadLegacy", NS, ASM);
            CollectionAssert.AreEquivalent(new[] { old }, pending.Keys);
            CollectionAssert.AreEqual(new[] { broken }, pending[old]);
        }

        [Test]
        public void ToAssetPath_MapsBackFromDisk()
        {
            Assert.AreEqual(FOLDER + "/Host.asset", MigrationScanner.ToAssetPath(Absolute(FOLDER + "/Host.asset")));
            Assert.IsNull(MigrationScanner.ToAssetPath(Path.Combine(Path.GetTempPath(), "Outside.asset")));
        }

        private static void AssertVariantRecovered(string variantPath)
        {
            var host = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath).GetComponent<TestBehaviour>();
            Assert.IsFalse(SerializationUtility.HasManagedReferencesWithMissingTypes(host));

            var single = host.Single as RenamedPayload;
            Assert.IsNotNull(single, "variant override on Single must survive");
            Assert.AreEqual(2, single.Value);
            Assert.AreEqual("variant-single", single.Label);

            var added = host.Many.Last() as RenamedPayload;
            Assert.IsNotNull(added, "element the variant added must survive");
            Assert.AreEqual("variant-added", added.Label);
        }

        private static void AssertSceneRecovered(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var host = scene.GetRootGameObjects().Select(root => root.GetComponent<TestBehaviour>()).Single(found => found != null);
            Assert.IsFalse(SerializationUtility.HasManagedReferencesWithMissingTypes(host));

            var single = host.Single as RenamedPayload;
            Assert.IsNotNull(single, "scene override on Single must survive");
            Assert.AreEqual("scene-single", single.Label);

            var labels = host.Many.OfType<RenamedPayload>().Select(payload => payload.Label).ToList();
            CollectionAssert.Contains(labels, "base-list-a", "from the base prefab");
            CollectionAssert.Contains(labels, "variant-added", "added by the variant");
            CollectionAssert.Contains(labels, "scene-added", "added by the scene instance");
            Assert.IsNotNull(host.Many.OfType<StablePayload>().FirstOrDefault(), "type that never moved");
        }

        private static GameObject CreatePrefab(string assetPath)
        {
            var go = new GameObject("Host");
            var host = go.AddComponent<TestBehaviour>();
            host.Single = new RenamedPayload { Value = 1, Label = "base-single" };
            host.Many.Add(new RenamedPayload { Value = 10, Label = "base-list-a" });
            host.Many.Add(new StablePayload { Note = "stable" });

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateVariant(GameObject basePrefab, string assetPath)
        {
            var instance = (GameObject) PrefabUtility.InstantiatePrefab(basePrefab);
            var host = instance.GetComponent<TestBehaviour>();
            host.Single = new RenamedPayload { Value = 2, Label = "variant-single" };
            host.Many.Add(new RenamedPayload { Value = 20, Label = "variant-added" });
            PrefabUtility.RecordPrefabInstancePropertyModifications(host);

            var variant = PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
            Object.DestroyImmediate(instance);

            Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(variant));
            return variant;
        }

        private static void CreateScene(GameObject variant, string scenePath)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var instance = (GameObject) PrefabUtility.InstantiatePrefab(variant, scene);
            var host = instance.GetComponent<TestBehaviour>();
            host.Single = new RenamedPayload { Value = 3, Label = "scene-single" };
            host.Many.Add(new RenamedPayload { Value = 30, Label = "scene-added" });
            PrefabUtility.RecordPrefabInstancePropertyModifications(host);

            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
