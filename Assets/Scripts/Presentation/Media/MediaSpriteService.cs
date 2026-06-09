using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Domain;
using PuzzleFlow.Media;
using UnityEngine;

namespace PuzzleFlow.Presentation.Media
{
    public sealed class MediaSpriteService : IMediaSpriteService
    {
        private readonly IMediaService mediaService;
        private readonly Dictionary<MediaReference, Sprite> runtimeSpritesByReference =
            new Dictionary<MediaReference, Sprite>();

        public MediaSpriteService(IMediaService mediaService)
        {
            this.mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        }

        public async UniTask<Sprite> LoadSpriteAsync(
            MediaReference reference,
            CancellationToken cancellationToken = default)
        {
            if (runtimeSpritesByReference.TryGetValue(reference, out Sprite runtimeSprite) && runtimeSprite != null)
            {
                return runtimeSprite;
            }

            try
            {
                return await mediaService.LoadAsync<Sprite>(reference, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception spriteException)
            {
                try
                {
                    return await LoadRuntimeSpriteAsync(reference, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception textureException)
                {
                    throw new InvalidOperationException(
                        $"Failed to load media '{reference.Key}' as Sprite or Texture2D.",
                        new AggregateException(spriteException, textureException));
                }
            }
        }

        public void Dispose()
        {
            foreach (Sprite sprite in runtimeSpritesByReference.Values)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            runtimeSpritesByReference.Clear();
        }

        private async UniTask<Sprite> LoadRuntimeSpriteAsync(
            MediaReference reference,
            CancellationToken cancellationToken)
        {
            Texture2D texture = await mediaService.LoadAsync<Texture2D>(reference, cancellationToken);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            runtimeSpritesByReference[reference] = sprite;
            return sprite;
        }
    }
}

