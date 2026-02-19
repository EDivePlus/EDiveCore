// Author: Radim Holub
// Created: 19.02.2026

using System;
using Newtonsoft.Json;

namespace EDIVE.UserCenter
{
    public interface IJsonCodec
    {
        string Serialize<T>(T value);
        bool TryDeserialize<T>(string json, out T value, out string error);
    }

    public sealed class NewtonsoftJsonCodec : IJsonCodec
    {
        public string Serialize<T>(T value)
            => JsonConvert.SerializeObject(value);

        public bool TryDeserialize<T>(string json, out T value, out string error)
        {
            try
            {
                value = JsonConvert.DeserializeObject<T>(json);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                value = default;
                error = e.Message;
                return false;
            }
        }
    }
}
