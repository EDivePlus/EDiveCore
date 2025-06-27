// Author: František Holubec
// Created: 27.06.2025

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using EDIVE.NativeUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.Core.Restart
{
    public static class AppRestartUtility
    {
        [MenuItem("Tools/Restart App", priority = 200)]
        public static void Restart()
        {
            RestartAsync().Forget();
        }

        public static async UniTask RestartAsync()
        {
            DebugLite.Log("[AppRestart] Starting...");
            await UniTask.Yield(); // Wait for end of frame to ensure all synchronous operations are done
            var tempScene = SceneManager.CreateScene("Temp");
            SceneManager.SetActiveScene(tempScene);
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.depth = 100;

            await UniTask.Yield();
            await InvokeRestartAttributes();
            await UniTask.Yield();

            DebugLite.Log("[AppRestart] Reloading root scene");
            await SceneManager.LoadSceneAsync(0); // Load boot scene, will also unload TMP scene as we load in single mode

            DebugLite.Log("[AppRestart] Completed");
        }

        private static async UniTask InvokeRestartAttributes()
        {
            var members = GetMembersWithAttribute<AAppRestartAttribute>().OrderBy(x => x.Item2.Order).ToList();
            foreach (var (member, attribute) in members)
            {
                DebugLite.Log($"[AppRestart] ({attribute.Order}) {attribute.ActionName} '{member.ReflectedType.FullName}.{member.Name}'");
                try
                {
                    await attribute.Process(member);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private static IEnumerable<(MemberInfo, TAttribute)> GetMembersWithAttribute<TAttribute>() where TAttribute : Attribute
        {
            var coreAssembly = typeof(AppRestartBuildProcessor).Assembly;
            var coreAssemblyName = coreAssembly.GetName().Name;
            
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || assembly.IsNonUserAssembly())
                    continue;
                
                if (assembly != coreAssembly && !assembly.IsReferencingAssembly(coreAssemblyName))
                    continue;

                foreach (var type in assembly.GetTypes())
                {
                    var members = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                    foreach (var member in members)
                    {
                        var attr = member.GetCustomAttribute<TAttribute>(true);
                        if (attr != null)
                            yield return (member, attr);
                    }
                }
            }
        }
    }
}
