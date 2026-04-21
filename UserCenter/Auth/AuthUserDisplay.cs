// Author: Michal Petr
// Created: 20.04.2026

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.UserCenter.Auth
{
    public class AuthUserDisplay : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _UserIdText;
        
        [SerializeField]
        private TMP_Text _DisplayNameText;
        
        [SerializeField]
        private TMP_Text _EmailText;
        
        [SerializeField]
        private TMP_Text _RolesText;

        [SerializeField]
        [PropertySpace]
        private TMP_Text _AvatarInitialsText;
        
        [SerializeField]
        private Image _AvatarBackground;
        
        [SerializeField]
        [PropertySpace]
        private AToggleState _IsAnonymousToggle;

        private void OnEnable()
        {
            var userInfo = AuthStorage.GetUserInfo();
            if (userInfo != null)
                SetUserInfo(userInfo);
        }

        private void SetUserInfo(AuthUserInfo userInfo)
        {
            if (_DisplayNameText) _DisplayNameText.text = userInfo.Name;
            if (_EmailText) _EmailText.text = userInfo.Email;
            if (_RolesText) _RolesText.text = string.Join(", ", userInfo.Roles);
            if (_UserIdText) _UserIdText.text = userInfo.Id;
            
            if (_AvatarInitialsText) _AvatarInitialsText.text = !string.IsNullOrEmpty(userInfo.Name) ? GetInitials(userInfo.Name) : "?";
            if (_AvatarBackground) _AvatarBackground.color = ColorFromString(userInfo.Id);
            
            if (_IsAnonymousToggle) _IsAnonymousToggle.SetState(userInfo.IsAnonymous);
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
