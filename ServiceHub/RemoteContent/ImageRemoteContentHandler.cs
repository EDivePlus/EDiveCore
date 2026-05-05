// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.ServiceHub.RemoteContent
{
    public class ImageRemoteContentHandler : ARemoteContentHandler
    {
        [SerializeField]
        private RawImage _Image;

        private Texture2D _texture;

        public override bool IsValidFor(ContentItemInfo contentInfo) => contentInfo.MediaTypeKey == "image";

        protected override UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken)
        {
            if (_texture == null)
                _texture = new Texture2D(2, 2);

            if (!_texture.LoadImage(content.Bytes))
                throw new InvalidOperationException("Failed to decode image bytes");

            if (_Image != null)
                _Image.texture = _texture;

            return UniTask.CompletedTask;
        }

        protected void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}
