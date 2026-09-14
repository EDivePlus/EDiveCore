// Author: František Holubec
// Created: 15.09.2026

using System;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    [Serializable]
    public class IosSigningSettings
    {
        public const string TEAM_ID_VARIABLE = "IOS_TEAM_ID";

        [SerializeField]
        private string _DeveloperTeamID;

        public string DeveloperTeamID
        {
            get
            {
                var fromEnvironment = Environment.GetEnvironmentVariable(TEAM_ID_VARIABLE);
                return string.IsNullOrEmpty(fromEnvironment) ? _DeveloperTeamID : fromEnvironment;
            }
        }

        public void Apply() => PlayerSettings.iOS.appleDeveloperTeamID = DeveloperTeamID ?? string.Empty;

        public void LoadCurrent() => _DeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID;

        public bool Validate() => true;
    }
}
