// Author: Michal Petr
// Created: 16.03.2026

namespace EDIVE.UserCenter.SaveData
{
    public interface ISaveDataLocalStore
    {
        bool TryGet(string key, out string json);
        void Set(string key, string json);
        void Delete(string key);
    }
}
