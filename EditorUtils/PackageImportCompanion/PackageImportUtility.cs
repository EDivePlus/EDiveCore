// Inspired by https://github.com/Code-Stage/Package2Folder

using System;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace EDIVE.EditorUtils.PackageImportCompanion
{
	public static class PackageImportUtility
	{
		private delegate object[] ExtractAndPrepareAssetListDelegate(string packagePath, out string packageIconPath, out string packageManagerDependenciesPath);

		private static Type _packageUtilityType;
		private static Type PackageUtilityType => _packageUtilityType ??= 
			typeof(MenuItem).Assembly.GetType("UnityEditor.PackageUtility");

		private static FieldInfo _destinationAssetPathFieldInfo;
		private static FieldInfo DestinationAssetPathFieldInfo => _destinationAssetPathFieldInfo ??= 
			typeof(MenuItem).Assembly.GetType("UnityEditor.ImportPackageItem").GetField("destinationAssetPath");

		private static MethodInfo _importPackageAssetsMethodInfo;
		private static MethodInfo ImportPackageAssetsMethodInfo => _importPackageAssetsMethodInfo ??= 
			PackageUtilityType.GetMethod("ImportPackageAssets");

		private static MethodInfo _importPackageAssetsWithOriginMethodInfo;
		private static MethodInfo ImportPackageAssetsWithOriginMethodInfo => _importPackageAssetsWithOriginMethodInfo ??= 
			PackageUtilityType.GetMethod("ImportPackageAssetsWithOrigin");

		private static MethodInfo _showImportPackageMethodInfo;
		private static MethodInfo ShowImportPackageMethodInfo => _showImportPackageMethodInfo ??= 
			PackageImportType.GetMethod("ShowImportPackage");

		private static Type _packageImportType;
		private static Type PackageImportType => _packageImportType ??= 
			typeof(MenuItem).Assembly.GetType("UnityEditor.PackageImport");

		private static FieldInfo _importPackageItemsFieldInfo;
		private static FieldInfo ImportPackageItemsFieldInfo => _importPackageItemsFieldInfo ??= 
			PackageImportType.GetField("m_ImportPackageItems", BindingFlags.NonPublic | BindingFlags.Instance);

		private static FieldInfo _treeFieldInfo;
		private static FieldInfo TreeFieldInfo => _treeFieldInfo ??= 
			PackageImportType.GetField("m_Tree", BindingFlags.NonPublic | BindingFlags.Instance);
		
		private static ExtractAndPrepareAssetListDelegate _extractAndPrepareAssetList;
		private static ExtractAndPrepareAssetListDelegate ExtractAndPrepareAssetList
		{
			get
			{
				if (_extractAndPrepareAssetList == null)
				{
					var method = PackageUtilityType.GetMethod("ExtractAndPrepareAssetList");
					if (method == null)
						throw new Exception("Couldn't extract method with ExtractAndPrepareAssetListDelegate delegate!");

					_extractAndPrepareAssetList = (ExtractAndPrepareAssetListDelegate)Delegate.CreateDelegate(
						typeof(ExtractAndPrepareAssetListDelegate),
						null,
						method);
				}

				return _extractAndPrepareAssetList;
			}
		}

		// PackageImport window watcher
		[InitializeOnLoadMethod]
		private static void SetupPackageImportWatcher()
		{
			EditorApplication.update -= WatchForPackageImportWindows;
			EditorApplication.update += WatchForPackageImportWindows;
		}

		private static double _nextWatchTime;
		private static EditorWindow _lastFocusedWindow;

		private static void WatchForPackageImportWindows()
		{
			var current = EditorWindow.focusedWindow;
			if (current == null || current == _lastFocusedWindow)
				return;
			
			_lastFocusedWindow = current;
			if (PackageImportType.IsInstanceOfType(EditorWindow.focusedWindow))
			{
				PackageImportCompanionWindow.ShowForImportWindow(current);
			}
		}
		
		// Unity Editor menu integration
		[MenuItem("Assets/Import Package/To Selected Folder", true)]
		private static bool IsImportToFolderCheck()
		{
			var selectedFolderPath = GetCurrentFolderPath();
			return !string.IsNullOrEmpty(selectedFolderPath);
		}

		[MenuItem("Assets/Import Package/To Selected Folder", false)]
		private static void Package2FolderCommand()
		{
			var packagePath = EditorUtility.OpenFilePanel("Import package ...", "",  "unitypackage");
			if (string.IsNullOrEmpty(packagePath)) return;
			if (!File.Exists(packagePath)) return;

			var selectedFolderPath = GetCurrentFolderPath();
			ImportPackageToFolder(packagePath, selectedFolderPath, true);
		}
		
		/// <summary>
		/// Allows to import package to the specified folder either via standard import window or silently.
		/// </summary>
		/// <param name="packagePath">Native path to the package.</param>
		/// <param name="selectedFolderPath">Path to the target folder where you wish to import package into.
		/// Relative to the project folder (should start with 'Assets')</param>
		/// <param name="interactive">If true - imports using standard import window, otherwise does this silently.</param>
		/// <param name="assetOrigin">An optional UnityEditor.AssetOrigin object which Unity from version 2023+ uses internally to store the source of the imported asset inside the meta file.</param>
		public static void ImportPackageToFolder(string packagePath, string selectedFolderPath, bool interactive, object assetOrigin = null)
		{
			var assetsItems = ExtractAndPrepareAssetList(packagePath, out var packageIconPath, out _);
			if (assetsItems == null) return;
			foreach (var item in assetsItems)
			{
				ChangeAssetItemPath(item, selectedFolderPath);
			}

			if (interactive)
			{
				ShowImportPackageWindow(packagePath, assetsItems, packageIconPath, assetOrigin);
			}
			else
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(packagePath);
				ImportPackageSilently(fileNameWithoutExtension, assetsItems, assetOrigin);
			}
		}

		public static void ChangeAssetItemPath(object assetItem, string selectedFolderPath)
		{
			if (string.IsNullOrEmpty(selectedFolderPath) || !selectedFolderPath.StartsWith("Assets"))
				throw new ArgumentException("selectedFolderPath must start with 'Assets'", nameof(selectedFolderPath));

			var destinationPath = (string)DestinationAssetPathFieldInfo.GetValue(assetItem);
			if (destinationPath.StartsWith("Packages/")) return;

			var firstSlashIndex = destinationPath.IndexOf('/');
			if (firstSlashIndex >= 0)
			{
				var relativePath = destinationPath[firstSlashIndex..];
				destinationPath = selectedFolderPath + relativePath;
			}
			else
			{
				destinationPath = selectedFolderPath + "/" + destinationPath;
			}

			DestinationAssetPathFieldInfo.SetValue(assetItem, destinationPath);
		}
		
		public static void ShowImportPackageWindow(string path, object[] array, string packageIconPath, object assetOrigin = null)
		{
			var productId = 0;
			string packageName = null;
			string packageVersion = null;
			var uploadId = 0;
			if (assetOrigin != null) {
				var assetOriginType = Type.GetType("UnityEditor.AssetOrigin, UnityEditor.CoreModule");
				if (assetOriginType != null)
				{
					var productIdProp = assetOriginType.GetField("productId");
					var packageVersionProp = assetOriginType.GetField("packageVersion");
					var packageNameProp = assetOriginType.GetField("packageName");
					var uploadIdProp = assetOriginType.GetField("uploadId");

					if (productIdProp != null) productId = productIdProp.GetValue(assetOrigin) as int? ?? 0;
					if (packageVersionProp != null) packageVersion = packageVersionProp.GetValue(assetOrigin) as string;
					if (packageNameProp != null) packageName = packageNameProp.GetValue(assetOrigin) as string;
					if (uploadIdProp != null) uploadId = uploadIdProp.GetValue(assetOrigin) as int? ?? 0;
				}
			}
			ShowImportPackageMethodInfo.Invoke(null, new object[]
			{
				path, array, packageIconPath, productId, packageName, packageVersion, uploadId
			});
		}

		public static void ImportPackageSilently(string packageName, object[] assetsItems, object assetOrigin = null)
		{
			if (assetOrigin != null)
			{
				ImportPackageAssetsWithOriginMethodInfo.Invoke(null, new[] {assetOrigin, assetsItems});
			}
			else
			{
				ImportPackageAssetsMethodInfo.Invoke(null, new object[] {packageName, assetsItems});
			}
		}
		
		public static object[] GetImportPackageItems(EditorWindow importWindow)
		{
			return ImportPackageItemsFieldInfo.GetValue(importWindow) as object[];
		}

		public static string[] GetImportItemPaths(EditorWindow importWindow)
		{
			var items = GetImportPackageItems(importWindow);
			if (items == null) return null;

			var paths = new string[items.Length];
			for (var i = 0; i < items.Length; i++)
			{
				paths[i] = (string)DestinationAssetPathFieldInfo.GetValue(items[i]);
			}
			return paths;
		}

		public static void SetImportWindowFolder(EditorWindow importWindow, string selectedFolderPath, string[] originalPaths)
		{
			var items = GetImportPackageItems(importWindow);
			if (items == null) return;

			// Restore original paths first to avoid stacking folder prefixes
			if (originalPaths != null)
			{
				for (var i = 0; i < items.Length && i < originalPaths.Length; i++)
				{
					DestinationAssetPathFieldInfo.SetValue(items[i], originalPaths[i]);
				}
			}

			// Apply the new folder
			foreach (var item in items)
			{
				ChangeAssetItemPath(item, selectedFolderPath);
			}

			// Reset tree view to force rebuild
			TreeFieldInfo.SetValue(importWindow, null);
			importWindow.Repaint();
		}
		
		public static void SetImportWindowPaths(EditorWindow importWindow, string[] paths)
		{
			var items = GetImportPackageItems(importWindow);
			if (items == null) return;
			
			if (paths != null)
			{
				for (var i = 0; i < items.Length && i < paths.Length; i++)
				{
					DestinationAssetPathFieldInfo.SetValue(items[i], paths[i]);
				}
			}
			
			TreeFieldInfo.SetValue(importWindow, null);
			importWindow.Repaint();
		}

		public static string GetCurrentFolderPath()
		{
			if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
				return null;

			var assetGuid = Selection.assetGUIDs[0];
			var path = AssetDatabase.GUIDToAssetPath(assetGuid);
			return !Directory.Exists(path) ? null : path;
		}
	}
}
