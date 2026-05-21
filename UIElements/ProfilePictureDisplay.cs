// Author: Michal Petr
// Created: 21.05.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UIElements
{
    public class ProfilePictureDisplay : MonoBehaviour
    {
        [SerializeField]
        [PropertySpace]
        private TMP_Text _InitialsText;
        
        [SerializeField]
        private Image _Background;

        public void SetProfilePicture(string initials, Color backgroundColor)
        {
            if (_InitialsText) _InitialsText.text = initials;
            if (_Background) _Background.color = backgroundColor;
        }
        
        public void SetProfilePictureFromName(string username)
        {
            var initials = GetInitials(username);
            var color = ColorFromString(username);
            SetProfilePicture(initials, color);
        }
        
        private static string GetInitials(string name)
        {
            var parts = name.Split(' ');
            return parts.Length == 1 ? parts[0][..1].ToUpper() : (parts[0][..1] + parts[1][..1]).ToUpper();
        }
        
        private static Color ColorFromString(string input)
        {
            var hash = Mathf.Abs(input.GetHashCode());
            var hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.6f, 0.45f);
        }
    }
}
