// Author: František Holubec
// Created: 28.10.2026

#if DEBUG_MENU
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDIVE.Debugging;
using EDIVE.External.DomainReloadHelper;
using Newtonsoft.Json;

namespace EDIVE.ServiceHub.SaveData
{
    // Save data the server refused, the page only shows while there is some
    public static class SaveDataDebugMenu
    {
        private const string PANEL = "Save data";

        [ClearOnReload]
        private static SaveDataService _service;

        [ClearOnReload]
        private static bool _registered;

        [ClearOnReload(NewInstance = true)]
        private static readonly List<DebugSection> SECTIONS = new();

        public static void Register(SaveDataService service)
        {
            if (!_registered)
                SaveDataRejections.Changed += Rebuild;
            _registered = true;
            _service = service;
            DebugMenu.Panel(PANEL).Icon("cloud_off");
            Rebuild();
        }

        private static void Rebuild()
        {
            SECTIONS.ForEach(s => s.Clear());
            SECTIONS.Clear();
            var rejections = SaveDataRejections.All.OrderBy(r => r.Time).ToList();
            if (rejections.Count == 0)
                return;

            var summary = DebugMenu.Section(PANEL, "Summary");
            summary.Watch("Not saved", () => SaveDataRejections.All.Count());
            summary.Button("Retry all", RetryAll).Primary();
            SECTIONS.Add(summary);

            foreach (var rejection in rejections)
            {
                var section = DebugMenu.Section(PANEL, $"{rejection.Key} ({rejection.Domain})");
                section.Watch("Reason", () => rejection.Reason);
                section.Watch("Size", () => GetSize(rejection));
                section.Watch("Since", () => rejection.Time.ToLocalTime().ToString("HH:mm:ss"));
                section.Button("Retry", () => Retry(rejection));
                SECTIONS.Add(section);
            }
        }

        private static void RetryAll() => SaveDataRejections.All.ToList().ForEach(Retry);

        // Sends the copy kept on this device again
        private static void Retry(SaveDataRejection rejection)
        {
            if (!TryGetCached(rejection, out var saveDataObject))
            {
                DebugMenu.Toast($"{rejection.Key} isn't loaded, can't retry", DebugToastLevel.Warning);
                return;
            }
            saveDataObject.SetDirty(SaveDataDirtyFlag.Immediate);
        }

        private static string GetSize(SaveDataRejection rejection)
        {
            if (!TryGetCached(rejection, out var saveDataObject))
                return "-";
            var bytes = Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(saveDataObject));
            return bytes >= 1024 * 1024 ? $"{bytes / (1024f * 1024f):0.0} MB" : $"{bytes / 1024f:0.0} KB";
        }

        private static bool TryGetCached(SaveDataRejection rejection, out ASaveDataObject saveDataObject)
        {
            saveDataObject = null;
            if (_service == null)
                return false;
            var domain = rejection.Domain == _service.User.Key ? _service.User : rejection.Domain == _service.Server.Key ? _service.Server : null;
            return domain != null && domain.TryGetCached(rejection.Key, out saveDataObject);
        }
    }
}
#endif
