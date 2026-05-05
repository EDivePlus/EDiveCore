// Author: Michal Petr
// Created: 05.05.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent.MeshLoaders
{
    public interface IMeshLoader
    {
        bool CanHandle(ContentItemInfo contentInfo);
        UniTask<GameObject> LoadAsync(byte[] data, Transform parent, CancellationToken cancellationToken);
    }
}
