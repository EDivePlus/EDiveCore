// Author: František Holubec
// Created: 14.04.2025

using EDIVE.AssetTranslation;
using Mirror;
using UnityEngine.Scripting;

namespace EDIVE.SceneManagement
{
    public class SceneDefinitionTranslator : ADefinitionTranslator<ASceneDefinition> { }

#if MIRROR
    [Preserve]
    public static class SceneDefinitionTranslatorMirrorSerialization
    {
        [Preserve]
        public static void WriteSceneDefinition(this NetworkWriter writer, ASceneDefinition value) => writer.WriteDefinition(value);

        [Preserve]
        public static ASceneDefinition ReadSceneDefinition(this NetworkReader reader) => reader.ReadDefinition<ASceneDefinition>();
    }
#endif
}
