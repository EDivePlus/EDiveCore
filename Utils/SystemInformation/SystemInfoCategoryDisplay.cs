// Author: František Holubec
// Created: 03.06.2026

using System.Text;
using EDIVE.NativeUtils;
using TMPro;
using UnityEngine;

namespace EDIVE.Utils.SystemInformation
{
    public class SystemInfoCategoryDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _NameText;
        
        [SerializeField]
        private TMP_Text _ValueText;
        
        public void SetCategory(SystemInfoCategory category, float emIndent = 2)
        {
            if (_NameText)
                _NameText.text = category.Name;

            if (_ValueText)
            {
                var sb = new StringBuilder();
                foreach (var entry in category.Entries)
                {
                    sb.Append($"{entry.Name}:");
                    sb.Append(entry.GetValue().Indent(emIndent, RichText.Unit.Em));
                    sb.AppendLine();
                }

                _ValueText.text = sb.ToString();
            }
        }
    }
}
