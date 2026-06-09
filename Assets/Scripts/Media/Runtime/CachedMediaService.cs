using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Media
{
    public sealed class CachedMediaService : IMediaService
    {
        private readonly IMediaSource source;
        private readonly Dictionary<MediaCacheKey, CacheEntry> entriesByKey = new Dictionary<MediaCacheKey, CacheEntry>();
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool isDisposed;

        public CachedMediaService(IMediaSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public UniTask<TAsset> LoadAsync<TAsset>(
            MediaReference reference,
            CancellationToken cancellationToken = default)
            where TAsset : UnityEngine.Object
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(CachedMediaService));
            }

            if (reference.IsEmpty)
            {
                throw new ArgumentException("Media reference is empty.", nameof(reference));
            }

            MediaCacheKey key = new MediaCacheKey(typeof(TAsset), reference.Key);
            if (!entriesByKey.TryGetValue(key, out CacheEntry entry))
            {
                entry = new CacheEntry(CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token));
                entriesByKey[key] = entry;
                LoadAndCacheAsync<TAsset>(key, entry, reference, entry.LoadToken).Forget();
            }

            entry.Retain();
            return AwaitTypedAssetAsync<TAsset>(key, entry, entry.Completion.Task, cancellationToken);
        }

        public void Clear()
        {
            if (isDisposed)
            {
                return;
            }

            lifetime.Cancel();
            lifetime.Dispose();
            lifetime = new CancellationTokenSource();

            List<CacheEntry> entries = new List<CacheEntry>(entriesByKey.Values);
            entriesByKey.Clear();

            for (int index = 0; index < entries.Count; index++)
            {
                CacheEntry entry = entries[index];
                entry.Evict();
                entry.Lease?.Dispose();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            lifetime.Cancel();
            List<CacheEntry> entries = new List<CacheEntry>(entriesByKey.Values);
            entriesByKey.Clear();

            for (int index = 0; index < entries.Count; index++)
            {
                CacheEntry entry = entries[index];
                entry.Evict();
                entry.Lease?.Dispose();
            }

            lifetime.Dispose();
            source.Dispose();
        }

        private async UniTask LoadAndCacheAsync<TAsset>(
            MediaCacheKey key,
            CacheEntry entry,
            MediaReference reference,
            CancellationToken cancellationToken)
            where TAsset : UnityEngine.Object
        {
            try
            {
                MediaLoadResult<TAsset> result = await source.LoadAsync<TAsset>(reference, cancellationToken);
                if (entry.IsEvicted || isDisposed)
                {
                    result.Lease?.Dispose();
                    throw new OperationCanceledException(cancellationToken);
                }

                entry.Asset = result.Asset;
                entry.Lease = result.Lease;
                entry.IsCompleted = true;
                entry.Completion.TrySetResult(result.Asset);
            }
            catch (OperationCanceledException)
            {
                RemoveEntry(key, entry);
                entry.IsCompleted = true;
                entry.Completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                RemoveEntry(key, entry);
                entry.IsCompleted = true;
                entry.Completion.TrySetException(exception);
            }
            finally
            {
                entry.DisposeLoadCancellation();
            }
        }

        private async UniTask<TAsset> AwaitTypedAssetAsync<TAsset>(
            MediaCacheKey key,
            CacheEntry entry,
            UniTask<UnityEngine.Object> task,
            CancellationToken cancellationToken)
            where TAsset : UnityEngine.Object
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                UnityEngine.Object asset = cancellationToken.CanBeCanceled
                    ? await task.AttachExternalCancellation(cancellationToken)
                    : await task;
                if (asset == null)
                {
                    throw new InvalidOperationException($"Media task completed with null {typeof(TAsset).Name} asset.");
                }

                return (TAsset)asset;
            }
            finally
            {
                ReleaseWaiter(key, entry);
            }
        }

        private void ReleaseWaiter(MediaCacheKey key, CacheEntry entry)
        {
            if (entry.Release() > 0 || entry.IsCompleted || entry.IsEvicted || isDisposed)
            {
                return;
            }

            if (RemoveEntry(key, entry))
            {
                entry.Evict();
            }
        }

        private bool RemoveEntry(MediaCacheKey key, CacheEntry entry)
        {
            if (entriesByKey.TryGetValue(key, out CacheEntry currentEntry) &&
                ReferenceEquals(currentEntry, entry))
            {
                entriesByKey.Remove(key);
                return true;
            }

            return false;
        }

        private readonly struct MediaCacheKey : IEquatable<MediaCacheKey>
        {
            private readonly Type assetType;
            private readonly string key;

            public MediaCacheKey(Type assetType, string key)
            {
                this.assetType = assetType;
                this.key = key;
            }

            public bool Equals(MediaCacheKey other)
            {
                return assetType == other.assetType &&
                       string.Equals(key, other.key, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MediaCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((assetType != null ? assetType.GetHashCode() : 0) * 397) ^
                           (key != null ? StringComparer.Ordinal.GetHashCode(key) : 0);
                }
            }
        }

        private sealed class CacheEntry
        {
            private readonly CancellationTokenSource loadCancellation;
            private bool isLoadCancellationDisposed;
            private int waiterCount;

            public CacheEntry(CancellationTokenSource loadCancellation)
            {
                this.loadCancellation = loadCancellation;
            }

            public readonly UniTaskCompletionSource<UnityEngine.Object> Completion =
                new UniTaskCompletionSource<UnityEngine.Object>();
            public UnityEngine.Object Asset;
            public IDisposable Lease;
            public bool IsEvicted;
            public bool IsCompleted;
            public CancellationToken LoadToken => loadCancellation.Token;

            public void Retain()
            {
                waiterCount++;
            }

            public int Release()
            {
                if (waiterCount > 0)
                {
                    waiterCount--;
                }

                return waiterCount;
            }

            public void Evict()
            {
                IsEvicted = true;
                CancelLoad();
            }

            public void CancelLoad()
            {
                if (isLoadCancellationDisposed)
                {
                    return;
                }

                try
                {
                    loadCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            public void DisposeLoadCancellation()
            {
                if (isLoadCancellationDisposed)
                {
                    return;
                }

                isLoadCancellationDisposed = true;
                loadCancellation.Dispose();
            }
        }
    }
}

