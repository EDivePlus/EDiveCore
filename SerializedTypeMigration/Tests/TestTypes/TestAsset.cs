using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.SerializedTypeMigration.Tests
{
    // class name = file name, or Unity loses the MonoScript
    public class TestAsset : ScriptableObject
    {
        [SerializeReference] public TestPayload Single;
        [SerializeReference] public List<TestPayload> Many = new();
    }
}
