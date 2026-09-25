using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.Json
{
    [Serializable]
    [HideLabel]
    [InlineProperty]
    public class JsonFileWatchingEditor<T> : IDisposable where T : class, new()
    {
        [ShowInInspector]
        [HideLabel]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [OnValueChanged(nameof(SaveData), true)]
        [BoxGroup("Data", GroupName = "@$property.Parent.NiceName")]
        public T Data { get; private set; }

        public string FilePath { get; }
        private FileSystemWatcher _fileWatcher;
        private JsonSerializerSettings _jsonSerializerSettings;
        private SynchronizationContext _mainContext;

        // Last content read or written. The watcher also fires for our own saves.
        private string _lastJson;

        public JsonFileWatchingEditor(string filePath, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            FilePath = filePath;
            _jsonSerializerSettings = jsonSerializerSettings;
            _mainContext = SynchronizationContext.Current;
            LoadData();
            SetupFileWatcher();
        }

        private void LoadData()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;

            if (!File.Exists(FilePath))
            {
                Data = new T();
                SaveData();
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(FilePath, Encoding.UTF8);
            }
            catch (IOException)
            {
                // Still being written, the next change event reads it
                return;
            }

            if (json == _lastJson)
                return;

            _lastJson = json;
            if (TryDeserialize(json, out var data))
            {
                Data = data;
                return;
            }

            Data = new T();
            SaveData();
        }

        private bool TryDeserialize(string json, out T data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            var trimmed = json.TrimStart();
            if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
                return false;

            try
            {
                data = JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        public void SaveData()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            try
            {
                var json = JsonConvert.SerializeObject(Data, _jsonSerializerSettings);
                _lastJson = json;
                File.WriteAllText(FilePath, json, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SetupFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath)!, Path.GetFileName(FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }

        // Watcher fires on its own thread
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_mainContext != null)
                _mainContext.Post(_ => ReloadData(), null);
            else
                ReloadData();
        }

        public void ReloadData()
        {
            LoadData();
        }

        public void Dispose()
        {
            if (_fileWatcher == null) return;
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Dispose();
        }
    }
}
