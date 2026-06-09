using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Domain;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PuzzleFlow.Media
{
    public sealed class AddressablesMediaSource : IMediaSource
    {
        public async UniTask<MediaLoadResult<TAsset>> LoadAsync<TAsset>(
            MediaReference reference,
            CancellationToken cancellationToken = default)
            where TAsset : UnityEngine.Object
        {
            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(reference.Key);
            try
            {
                TAsset asset = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
                {
                    throw new InvalidOperationException($"Failed to load media '{reference.Key}' as {typeof(TAsset).Name}.");
                }

                return new MediaLoadResult<TAsset>(asset, new AddressablesMediaLease<TAsset>(handle));
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }
        }

        public void Dispose()
        {
        }

        private sealed class AddressablesMediaLease<TAsset> : IDisposable
            where TAsset : UnityEngine.Object
        {
            private AsyncOperationHandle<TAsset> handle;
            private bool isDisposed;

            public AddressablesMediaLease(AsyncOperationHandle<TAsset> handle)
            {
                this.handle = handle;
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}

