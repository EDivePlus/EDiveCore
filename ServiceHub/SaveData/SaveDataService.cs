// Author: Michal Petr
// Created: 12.05.2026

using EDIVE.OdinExtensions.Attributes;
using EDIVE.ServiceHub.Auth;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.ServiceHub.SaveData
{
    public class SaveDataService : MonoBehaviour, IServiceHubModule
    {
        [SerializeField]
        [EnhancedBoxGroup("User Domain", Color = "@ColorTools.Orange")]
        [InlineProperty]
        [HideLabel]
        private SaveDataDomain _User = new("user", new ASaveDataStore[]
        {
            new PlayerPrefsSaveDataStore(), 
            new ServiceHubSaveDataStore()
        });
        
        [SerializeField]
        [EnhancedBoxGroup("Server Domain", Color = "@ColorTools.Blue")]
        [InlineProperty]
        [HideLabel]
        private SaveDataDomain _Server = new("server", new ASaveDataStore[]
        {
            new PlayerPrefsSaveDataStore(), 
            new ServiceHubSaveDataStore()
        });
        
        public const int MAX_KEY_LENGTH = 256;
        public const int MAX_VALUE_BYTES = 256 * 1024;  // 256 KB
        public const int BATCH_MAX_SIZE = 128 * 1024;   // 128 KB
        
        public ServiceHubSettings Settings { get; private set; }

        public SaveDataDomain User => _User;
        public SaveDataDomain Server => _Server;

        public void Initialize(ServiceHubSettings settings)
        {
            Settings = settings;

            _User.Initialize(this, AuthStorage.Client);
            _Server.Initialize(this, AuthStorage.Server);
        }
    }
}
