// Author: František Holubec
// Created: 09.02.2026

using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Networking;

namespace EDIVE.UserCenter
{
    public class UserCenterManager : ALoadableServiceBehaviour<UserCenterManager>
    {
        [SerializeField]
        private string _EndpointUrl = "https://ediveplus.phil.muni.cz:8443";
        
        [SerializeField]
        private string _SaveDataContext = "ediveplus";
        
        [SerializeField]
        private string _SaveDataBranchId = "2";
        
        [SerializeField, Min(5)]
        private int _TimeoutSeconds = 60;
        
        [SerializeField]
        private string _ProfileKey = "player_profile_v1";

        protected override UniTask LoadRoutine(Action<float> progressCallback)
        {
            return UniTask.CompletedTask;
        }
        
        private string TokenOrNull() => AuthStorage.GetAccessToken();

        private UnityWebRequest BuildJsonReq(string method, string url, object bodyOrNull)
        {
            var req = new UnityWebRequest(url, method);
            if (bodyOrNull != null)
            {
                var json = JsonConvert.SerializeObject(bodyOrNull);
                var bytes = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.downloadHandler = new DownloadHandlerBuffer();

            var token = TokenOrNull();
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);
            if (!string.IsNullOrEmpty(_SaveDataBranchId))
                req.SetRequestHeader("branch-id", _SaveDataBranchId);

            req.timeout = Mathf.Max(10, _TimeoutSeconds);
            return req;
        }

        private string SavedataUrl(params string[] segments)
        {
            var baseu = $"{_EndpointUrl}/{_SaveDataContext}/savedata";
            if (segments != null && segments.Length > 0)
                return baseu + "/" + string.Join("/", segments);
            return baseu;
        }
        
        public System.Collections.IEnumerator LoadProfileFromSavedataAndApply(Action<bool, string> onDone = null)
        {
            // vezmeme list všech záznamů pro přihlášeného uživatele v branchi
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var req = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return req.SendWebRequest();

                var text = req.downloadHandler?.text ?? "";

                if (req.responseCode == 404)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] 404 → žádný uložený profil; ponechávám výchozí hodnoty.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][LIST] ERR {req.responseCode}: {req.error}\n{text}");
                    onDone?.Invoke(false, text);
                    yield break;
                }

                List<SavedataRecord> rows = null;

                // API může vracet buď prosté pole, nebo stránkované { content: [...] }
                try
                {
                    rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(text);
                }
                catch
                {
                }

                if (rows == null)
                {
                    try
                    {
                        var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(text);
                        rows = wrapper?.content;
                    }
                    catch
                    {
                    }
                }

                if (rows == null || rows.Count == 0)
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] prázdné – ponechávám výchozí.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }
                var myUuid = AuthStorage.GetUserId();
                if (!string.IsNullOrEmpty(myUuid))
                    rows = rows.FindAll(r =>
                        string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));


                var rec = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                if (rec == null || string.IsNullOrEmpty(rec.description))
                {
                    Debug.Log("[PROFILE/SAVEDATA][LIST] nenalezen klíč – ponechávám výchozí.");
                    onDone?.Invoke(true, "empty");
                    yield break;
                }

                ProfileJson pj = null;
                try
                {
                    pj = JsonConvert.DeserializeObject<ProfileJson>(rec.description);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[PROFILE/SAVEDATA][PARSE] {e.Message}");
                }

                if (pj == null)
                {
                    onDone?.Invoke(false, "Invalid profile JSON in description.");
                    yield break;
                }

                // TODO make this UniTask and await it
                // var prof = PlayerProfile;
                // if (!string.IsNullOrWhiteSpace(pj.username)) prof.username = pj.username;
                // if (!string.IsNullOrWhiteSpace(pj.avatarId))
                // {
                //     _lastSelectedAvatarId = pj.avatarId;
                //     prof.avatar = pj.avatarId;
                // }

                // Debug.Log($"[PROFILE/SAVEDATA] Applied username='{prof.username}', avatarId='{prof.avatar}'");
                onDone?.Invoke(true, "ok");
            }
        }

        public System.Collections.IEnumerator SaveProfileToSavedataUpsert(Action<bool, string> onDone = null)
        {
            // Todo feed pj as param
            ProfileJson pj = null;
            
            var descriptionJson = JsonConvert.SerializeObject(pj);

            // 1) LIST → najdi existující záznam podle _ProfileKey
            SavedataRecord existing = null;
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var sreq = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return sreq.SendWebRequest();

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    List<SavedataRecord> rows = null;
                    try
                    {
                        rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(stext);
                    }
                    catch
                    {
                    }

                    if (rows == null)
                    {
                        try
                        {
                            var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(stext);
                            rows = wrapper?.content;
                        }
                        catch
                        {
                        }
                    }

                    if (rows != null)
                    {
                        var myUuid = AuthStorage.GetUserId();
                        if (!string.IsNullOrEmpty(myUuid))
                            rows = rows.FindAll(r =>
                                string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));

                        existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            if (existing != null && existing.id > 0)
            {
                // 2) UPDATE (PUT)
                var putUrl = SavedataUrl(existing.id.ToString());
                var body = new {key = _ProfileKey, description = descriptionJson};
                using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPUT, putUrl, body))
                {
                    Debug.Log($"[PROFILE/SAVEDATA][UPDATE] PUT {putUrl} body={JsonConvert.SerializeObject(body)}");
                    yield return preq.SendWebRequest();

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][UPDATE] OK {preq.responseCode}: {ptext}");
                        onDone?.Invoke(true, ptext);
                    }
                    else
                    {
                        Debug.LogError($"[PROFILE/SAVEDATA][UPDATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                        onDone?.Invoke(false, ptext);
                    }
                }
            }
            else
            {
                // 3) CREATE (POST)
                var postUrl = SavedataUrl();
                var uuid = AuthStorage.GetUserId();

                object body = string.IsNullOrEmpty(uuid)
                    ? new { key = _ProfileKey, description = descriptionJson }
                    : new { key = _ProfileKey, description = descriptionJson, userUuid = uuid };

                using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPOST, postUrl, body))
                {
                    preq.SetRequestHeader("Accept", "application/json"); // pomáhá některým backendům

                    Debug.Log($"[PROFILE/SAVEDATA][CREATE] POST {postUrl} body={JsonConvert.SerializeObject(body)}");
                    yield return preq.SendWebRequest();

                    var ptext = preq.downloadHandler?.text ?? "";
                    if (preq.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"[PROFILE/SAVEDATA][CREATE] OK {preq.responseCode}: {ptext}");
                        onDone?.Invoke(true, ptext);
                    }
                    else
                    {
                        Debug.LogError($"[PROFILE/SAVEDATA][CREATE] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                        onDone?.Invoke(false, ptext);
                    }
                }
            }
        }


        [Button("POST Save Profile (savedata upsert)")]
        [GUIColor(0.25f, 0.8f, 0.55f)]
        private void Btn_SaveProfile_Upsert()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(SaveProfileToSavedataUpsert());
        }

        [Button("GET Load Profile (from savedata)")]
        [GUIColor(0.4f, 0.7f, 1f)]
        private void Btn_LoadProfile_FromSavedata()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(LoadProfileFromSavedataAndApply((ok, msg) =>
            {
                Debug.Log(ok
                    ? "[PROFILE/SAVEDATA][LOAD] OK"
                    : "[PROFILE/SAVEDATA][LOAD] ERR: " + msg);
            }));
        }

        [Button("PUT Update Profile (savedata)")]
        [GUIColor(1f, 0.85f, 0.35f)]
        private void Btn_UpdateProfile_Put()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Spusť Play Mode.");
                return;
            }

            StartCoroutine(UpdateProfileViaPut());
        }

        private System.Collections.IEnumerator UpdateProfileViaPut(Action<bool, string> onDone = null)
        {
            // Todo Feed profile as param
            ProfileJson pj = null; 
            
            // LIST → najdi key
            SavedataRecord existing = null;
            var listUrl = SavedataUrl() + "?pgSize=250";
            using (var sreq = BuildJsonReq(UnityWebRequest.kHttpVerbGET, listUrl, null))
            {
                Debug.Log($"[PROFILE/SAVEDATA][LIST] GET {listUrl}");
                yield return sreq.SendWebRequest();

                var stext = sreq.downloadHandler?.text ?? "";
                if (sreq.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(stext))
                {
                    List<SavedataRecord> rows = null;
                    try
                    {
                        rows = JsonConvert.DeserializeObject<List<SavedataRecord>>(stext);
                    }
                    catch
                    {
                    }

                    if (rows == null)
                    {
                        try
                        {
                            var wrapper = JsonConvert.DeserializeObject<ContentWrapper<SavedataRecord>>(stext);
                            rows = wrapper?.content;
                        }
                        catch
                        {
                        }
                    }

                    if (rows != null)
                    {
                        var myUuid = AuthStorage.GetUserId();
                        if (!string.IsNullOrEmpty(myUuid))
                            rows = rows.FindAll(r =>
                                string.Equals(r.userUuid, myUuid, StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(r.userBasicPojo?.uuid, myUuid, StringComparison.OrdinalIgnoreCase));

                        existing = rows.Find(r => string.Equals(r.key, _ProfileKey, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            if (existing == null || existing.id == 0)
            {
                Debug.LogWarning("[PROFILE/SAVEDATA][PUT] Nenalezen existující profil — žádný update neproběhne.");
                onDone?.Invoke(false, "no existing");
                yield break;
            }


            var descriptionJson = JsonConvert.SerializeObject(pj);
            var putUrl = SavedataUrl(existing.id.ToString());
            var body = new {key = _ProfileKey, description = descriptionJson};

            using (var preq = BuildJsonReq(UnityWebRequest.kHttpVerbPUT, putUrl, body))
            {
                Debug.Log($"[PROFILE/SAVEDATA][PUT] {putUrl} body={JsonConvert.SerializeObject(body)}");
                yield return preq.SendWebRequest();

                var ptext = preq.downloadHandler?.text ?? "";
                if (preq.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[PROFILE/SAVEDATA][PUT] OK {preq.responseCode}: {ptext}");
                    onDone?.Invoke(true, ptext);
                }
                else
                {
                    Debug.LogError($"[PROFILE/SAVEDATA][PUT] ERR {preq.responseCode}: {preq.error}\n{ptext}");
                    onDone?.Invoke(false, ptext);
                }
            }
        }
    }
    
    [Serializable]
    public class SavedataRecord
    {
        public long id;
        public string key;
        public string description;
        public long userId;
        public long branchId;
        public string userUuid;
        public UserBasicPojo userBasicPojo;
    }
        
    [Serializable]
    public class UserBasicPojo
    {
        public string uuid;
        public string firstName;
        public string surname;
        public string username;
        public string userType;
        public string email;
    }
        
    [Serializable]
    public class ProfileJson
    {
        public string username;
        public string avatarId;
    }

    [Serializable]
    public class ContentWrapper<TItem>
    {
        public List<TItem> content;
    }
}
