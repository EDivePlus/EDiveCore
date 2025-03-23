using System;
using System.IO;
using System.Text;
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

        public JsonFileWatchingEditor(string filePath, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            FilePath = filePath;
            _jsonSerializerSettings = jsonSerializerSettings;
            LoadData();
            SetupFileWatcher();
        }

        private void LoadData()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            if (File.Exists(FilePath))
            {
                try
                {
                    var json = File.ReadAllText(FilePath, Encoding.UTF8);
                    Data = JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings) ?? new T();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Data = new T();
                }
            }
            else
            {
                Data = new T();
                SaveData();
            }
        }

        public void SaveData()
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            try
            {
                var json = JsonConvert.SerializeObject(Data, _jsonSerializerSettings);
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

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
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
