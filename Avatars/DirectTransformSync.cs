// Author: František Holubec
// Created: 12.06.2025
using UnityEngine;
using EDIVE.DataStructures.ScriptableVariables.Variables;
using Mirror;

namespace EDIVE.DataStructures.ScriptableVariables
{
    public class DirectTransformSync : NetworkBehaviour
    {
        [SerializeField]
        private AScriptableVariable<Transform> source;

        void Update()
        {
            if (isLocalPlayer)
            {
                if (source?.Value != null)
                {
                    transform.position = source.Value.position;
                    transform.rotation = source.Value.rotation;
                }
            }
        }
    }
}
