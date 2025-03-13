using System.IO;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using FilePathAttribute = Sirenix.OdinInspector.FilePathAttribute;

namespace EDIVE.OdinExtensions.Editor.Drawers
{
    [DrawerPriority(DrawerPriorityLevel.WrapperPriority)]
    [AllowGUIEnabledForReadonly]
    public class ShowOpenInExplorerAttributeDrawer : OdinAttributeDrawer<ShowOpenInExplorerAttribute, string>
    {
        private bool _isAbsolutePath;
        
        protected override void Initialize()
        {
            _isAbsolutePath = GetIsAbsolutePath();
        }

        private bool GetIsAbsolutePath()
        {
            var filePathAttribute = Property.GetAttribute<FilePathAttribute>();
            if (filePathAttribute != null)
            {
                return filePathAttribute.AbsolutePath;
            }
            var folderPathAttribute = Property.GetAttribute<FolderPathAttribute>();
            if (folderPathAttribute != null)
            {
                return folderPathAttribute.AbsolutePath;
            }
            return Attribute.IsAbsolutePath;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();
            {
                CallNextDrawer(label);
                GUIHelper.PushGUIEnabled(true);
                var rect = GUILayoutUtility.GetRect(18, 18, SirenixGUIStyles.Button,  GUILayoutOptions.ExpandWidth(false).Width(18));
                if (SirenixEditorGUI.IconButton(rect, FontAwesomeEditorIcons.SquareArrowUpRightSolid, "Open in file explorer"))
                {
                    var path = _isAbsolutePath ? ValueEntry.SmartValue : PathUtility.GetAbsolutePath(ValueEntry.SmartValue);
                    if (!File.Exists(path))
                    {
                        var directoryInfo = new DirectoryInfo(path);
                        while (directoryInfo != null && !Directory.Exists(directoryInfo.FullName) && directoryInfo.Parent != null)
                        {
                            directoryInfo = directoryInfo.Parent;
                        }

                        path = directoryInfo.FullName;
                    }
                
                    EditorUtility.RevealInFinder(path.Replace("\\","/"));
                }
                GUIHelper.PopGUIEnabled();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
