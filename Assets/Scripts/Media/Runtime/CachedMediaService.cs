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
        public const int DefaultMaxCompletedEntries = 64;

        private readonly IMediaSource source;
        private readonly int maxCompletedEntries;
        private readonly Dictionary<MediaCacheKey, CacheEntry> entriesByKey = new Dictionary<MediaCacheKey, CacheEntry>();
        private readonly LinkedList<MediaCacheKey> completedLru = new LinkedList<MediaCacheKey>();
        private CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool isDisposed;

        public CachedMediaService(IMediaSource source, int maxCompletedEntries = DefaultMaxCompletedEntries)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            if (maxCompletedEntries < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxCompletedEntries),
                    maxCompletedEntries,
                    "Completed media cache size cannot be negative.");
            }

            this.maxCompletedEntries = maxCompletedEntries;
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

            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled<TAsset>(cancellationToken);
            }

            MediaCacheKey key = new MediaCacheKey(typeof(TAsset), reference.Key);
            bool shouldStartLoad = false;
            if (!entriesByKey.TryGetValue(key, out CacheEntry entry))
            {
                entry = new CacheEntry(CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token));
                entriesByKey[key] = entry;
                shouldStartLoad = true;
            }
            else
            {
                TouchCompletedEntry(entry);
            }

            entry.Retain();
            if (shouldStartLoad)
            {
                LoadAndCacheAsync<TAsset>(key, entry, reference, entry.LoadToken).Forget();
            }

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

            EvictAllEntries();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            lifetime.Cancel();
            EvictAllEntries();

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

                entry.SetLoaded(result.Asset, result.Lease);
                TrackCompletedEntry(key, entry);
                entry.Completion.TrySetResult(result.Asset);
                TrimCompletedEntries();
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
            if (entry.Release() > 0 || entry.IsEvicted || isDisposed)
            {
                return;
            }

            if (!entry.IsCompleted)
            {
                if (RemoveEntry(key, entry))
                {
                    entry.Evict();
                }

                return;
            }

            TrimCompletedEntries();
        }

        private bool RemoveEntry(MediaCacheKey key, CacheEntry entry)
        {
            if (entriesByKey.TryGetValue(key, out CacheEntry currentEntry) &&
                ReferenceEquals(currentEntry, entry))
            {
                entriesByKey.Remove(key);
                UntrackCompletedEntry(entry);
                return true;
            }

            return false;
        }

        private void EvictAllEntries()
        {
            List<CacheEntry> entries = new List<CacheEntry>(entriesByKey.Values);
            entriesByKey.Clear();
            completedLru.Clear();

            for (int index = 0; index < entries.Count; index++)
            {
                entries[index].LruNode = null;
                entries[index].Evict();
            }
        }

        private void TrackCompletedEntry(MediaCacheKey key, CacheEntry entry)
        {
            UntrackCompletedEntry(entry);
            entry.LruNode = completedLru.AddLast(key);
        }

        private void TouchCompletedEntry(CacheEntry entry)
        {
            if (!entry.IsCompleted || entry.LruNode == null)
            {
                return;
            }

            completedLru.Remove(entry.LruNode);
            completedLru.AddLast(entry.LruNode);
        }

        private void UntrackCompletedEntry(CacheEntry entry)
        {
            if (entry.LruNode == null)
            {
                return;
            }

            if (entry.LruNode.List != null)
            {
                completedLru.Remove(entry.LruNode);
            }

            entry.LruNode = null;
        }

        private void TrimCompletedEntries()
        {
            while (completedLru.Count > maxCompletedEntries)
            {
                if (!TryEvictOldestIdleCompletedEntry())
                {
                    return;
                }
            }
        }

        private bool TryEvictOldestIdleCompletedEntry()
        {
            LinkedListNode<MediaCacheKey> node = completedLru.First;
            int checkedCount = completedLru.Count;
            for (int index = 0; index < checkedCount && node != null; index++)
            {
                MediaCacheKey key = node.Value;
                LinkedListNode<MediaCacheKey> next = node.Next;

                if (!entriesByKey.TryGetValue(key, out CacheEntry entry))
                {
                    completedLru.Remove(node);
                }
                else if (entry.CanEvictCompleted)
                {
                    RemoveEntry(key, entry);
                    entry.Evict();
                    return true;
                }

                node = next;
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
            public LinkedListNode<MediaCacheKey> LruNode;
            public CancellationToken LoadToken => loadCancellation.Token;
            public bool CanEvictCompleted => IsCompleted && waiterCount == 0 && !IsEvicted;

            public void SetLoaded(UnityEngine.Object asset, IDisposable lease)
            {
                Asset = asset;
                Lease = lease;
                IsCompleted = true;
            }

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
                if (IsEvicted)
                {
                    return;
                }

                IsEvicted = true;
                CancelLoad();
                DisposeLease();
                Asset = null;
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

            private void DisposeLease()
            {
                if (Lease == null)
                {
                    return;
                }

                Lease.Dispose();
                Lease = null;
            }
        }
    }
}

