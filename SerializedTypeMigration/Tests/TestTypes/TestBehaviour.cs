using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.SerializedTypeMigration.Tests
{
    public class TestBehaviour : MonoBehaviour
    {
        [SerializeReference] public TestPayload Single;
        [SerializeReference] public List<TestPayload> Many = new();
    }
}
