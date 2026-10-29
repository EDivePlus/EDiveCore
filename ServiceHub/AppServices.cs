// Author: František Holubec
// Created: 28.10.2026

using System;
using System.Collections.Generic;
using EDIVE.External.DomainReloadHelper;
using EDIVE.ServiceHub.Auth;

namespace EDIVE.ServiceHub
{
    [Flags]
    public enum AppService
    {
        None = 0,
        Lobby = 1 << 0,
        Servers = 1 << 1,
        Streams = 1 << 2,
        SaveData = 1 << 3,
        Content = 1 << 4
    }

    // Services the app has turned on in ServiceHub. Unknown until a sign-in lists them, then off unless listed
    public static class AppServices
    {
        [ClearOnReload]
        private static AppService _disabled;

        [ClearOnReload]
        private static HashSet<string> _listed;

        [ClearOnReload]
        private static bool _loaded;

        [ClearOnReload]
        public static event Action Changed;

        public static bool IsEnabled(AppService service)
        {
            if ((_disabled & service) != 0)
                return false;
            if (!_loaded)
                Load();
            return _listed == null || _listed.Contains(service.ToString());
        }

        // New user info, the server's list wins over services seen off before
        public static void Refresh()
        {
            _disabled = AppService.None;
            Load();
            Changed?.Invoke();
        }

        // ServiceDisabled from the service's endpoint
        public static bool HandleStatus(int apiStatus, AppService service)
        {
            if (service == AppService.None || apiStatus != (int) ServiceHubStatus.ServiceDisabled)
                return false;
            if ((_disabled & service) != 0)
                return true;
            _disabled |= service;
            Changed?.Invoke();
            return true;
        }

        private static void Load()
        {
            var services = AuthStorage.Client.HasSession ? AuthStorage.Client.GetInfo<AuthUserInfo>()?.AppServices : null;
            _listed = services != null ? new HashSet<string>(services) : null;
            _loaded = true;
        }
    }
}
