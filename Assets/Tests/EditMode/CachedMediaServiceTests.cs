using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using PuzzleFlow.Domain;
using PuzzleFlow.Media;
using UnityEngine;

namespace PuzzleFlow.Tests
{
    public sealed class CachedMediaServiceTests
    {
        [Test]
        public void LoadAsync_WhenTokenAlreadyCanceled_DoesNotStartSourceLoad()
        {
            ImmediateMediaSource source = new ImmediateMediaSource();
            CachedMediaService service = new CachedMediaService(source, 2);
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            try
            {
                Task<Texture2D> load = service.LoadAsync<Texture2D>(
                    new MediaReference("preview/canceled"),
                    cancellation.Token).AsTask();

                Assert.CatchAsync<OperationCanceledException>(async () => await load);
                Assert.That(source.GetLoadCount("preview/canceled"), Is.EqualTo(0));
            }
            finally
            {
                cancellation.Dispose();
                service.Dispose();
            }
        }

        [Test]
        public async Task LoadAsync_WhenEntryIsCached_ReusesLoadedAssetAndLease()
        {
            ImmediateMediaSource source = new ImmediateMediaSource();
            CachedMediaService service = new CachedMediaService(source, 2);
            CountingLease lease;

            try
            {
                MediaReference reference = new MediaReference("preview/a");

                Texture2D first = await service.LoadAsync<Texture2D>(reference);
                Texture2D second = await service.LoadAsync<Texture2D>(reference);
                lease = source.GetLease("preview/a", 0);

                Assert.That(second, Is.SameAs(first));
                Assert.That(source.GetLoadCount("preview/a"), Is.EqualTo(1));
                Assert.That(lease.DisposeCount, Is.EqualTo(0));
            }
            finally
            {
                service.Dispose();
            }

            Assert.That(lease.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LoadAsync_WhenCompletedCacheExceedsLimit_EvictsLeastRecentlyUsedIdleEntry()
        {
            ImmediateMediaSource source = new ImmediateMediaSource();
            CachedMediaService service = new CachedMediaService(source, 2);

            try
            {
                MediaReference firstReference = new MediaReference("preview/a");
                MediaReference secondReference = new MediaReference("preview/b");
                MediaReference thirdReference = new MediaReference("preview/c");

                Texture2D first = await service.LoadAsync<Texture2D>(firstReference);
                await service.LoadAsync<Texture2D>(secondReference);
                Texture2D firstAgain = await service.LoadAsync<Texture2D>(firstReference);
                await service.LoadAsync<Texture2D>(thirdReference);

                Assert.That(firstAgain, Is.SameAs(first));
                Assert.That(source.GetLoadCount("preview/a"), Is.EqualTo(1));
                Assert.That(source.GetLoadCount("preview/b"), Is.EqualTo(1));
                Assert.That(source.GetLease("preview/b", 0).DisposeCount, Is.EqualTo(1));
                Assert.That(source.GetLease("preview/a", 0).DisposeCount, Is.EqualTo(0));
                Assert.That(source.GetLease("preview/c", 0).DisposeCount, Is.EqualTo(0));

                await service.LoadAsync<Texture2D>(secondReference);

                Assert.That(source.GetLoadCount("preview/b"), Is.EqualTo(2));
                Assert.That(source.GetLease("preview/a", 0).DisposeCount, Is.EqualTo(1));
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public async Task LoadAsync_WhenLastWaiterCancelsBeforeCompletion_CancelsSourceAndAllowsRetry()
        {
            ControllableMediaSource source = new ControllableMediaSource();
            CachedMediaService service = new CachedMediaService(source, 2);
            CancellationTokenSource cancellation = new CancellationTokenSource();

            try
            {
                MediaReference reference = new MediaReference("preview/slow");
                Task<Texture2D> load = service.LoadAsync<Texture2D>(reference, cancellation.Token).AsTask();

                Assert.That(source.LoadCount, Is.EqualTo(1));
                cancellation.Cancel();

                Assert.CatchAsync<OperationCanceledException>(async () => await load);
                await WaitUntil(() => source.CancellationObservedCount == 1);

                Task<Texture2D> retry = service.LoadAsync<Texture2D>(reference).AsTask();
                Assert.That(source.LoadCount, Is.EqualTo(2));

                source.CompleteLatest();
                Texture2D texture = await retry;

                Assert.That(texture, Is.Not.Null);
            }
            finally
            {
                cancellation.Dispose();
                service.Dispose();
            }
        }

        private static async Task WaitUntil(Func<bool> predicate)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                if (predicate.Invoke())
                {
                    return;
                }

                await Task.Delay(10);
            }

            Assert.Fail("Condition was not met before timeout.");
        }

        private sealed class ImmediateMediaSource : IMediaSource
        {
            private readonly Dictionary<string, int> loadCountsByKey = new Dictionary<string, int>();
            private readonly Dictionary<string, List<CountingLease>> leasesByKey =
                new Dictionary<string, List<CountingLease>>();
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public UniTask<MediaLoadResult<TAsset>> LoadAsync<TAsset>(
                MediaReference reference,
                CancellationToken cancellationToken = default)
                where TAsset : UnityEngine.Object
            {
                cancellationToken.ThrowIfCancellationRequested();
                CountingLease lease = TrackLoad(reference.Key);
                Texture2D texture = new Texture2D(1, 1)
                {
                    name = reference.Key
                };
                assets.Add(texture);

                return UniTask.FromResult(new MediaLoadResult<TAsset>((TAsset)(UnityEngine.Object)texture, lease));
            }

            public int GetLoadCount(string key)
            {
                return loadCountsByKey.TryGetValue(key, out int count) ? count : 0;
            }

            public CountingLease GetLease(string key, int index)
            {
                return leasesByKey[key][index];
            }

            public void Dispose()
            {
                for (int index = 0; index < assets.Count; index++)
                {
                    if (assets[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(assets[index]);
                    }
                }

                assets.Clear();
            }

            private CountingLease TrackLoad(string key)
            {
                loadCountsByKey.TryGetValue(key, out int count);
                loadCountsByKey[key] = count + 1;

                if (!leasesByKey.TryGetValue(key, out List<CountingLease> leases))
                {
                    leases = new List<CountingLease>();
                    leasesByKey[key] = leases;
                }

                CountingLease lease = new CountingLease();
                leases.Add(lease);
                return lease;
            }
        }

        private sealed class ControllableMediaSource : IMediaSource
        {
            private readonly List<PendingLoad> pendingLoads = new List<PendingLoad>();
            private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();

            public int LoadCount { get; private set; }
            public int CancellationObservedCount { get; private set; }

            public UniTask<MediaLoadResult<TAsset>> LoadAsync<TAsset>(
                MediaReference reference,
                CancellationToken cancellationToken = default)
                where TAsset : UnityEngine.Object
            {
                LoadCount++;
                PendingLoad pendingLoad = new PendingLoad(reference.Key);
                pendingLoads.Add(pendingLoad);
                return WaitForCompletionAsync<TAsset>(pendingLoad, cancellationToken);
            }

            public void CompleteLatest()
            {
                pendingLoads[pendingLoads.Count - 1].Complete();
            }

            public void Dispose()
            {
                for (int index = 0; index < assets.Count; index++)
                {
                    if (assets[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(assets[index]);
                    }
                }

                assets.Clear();
            }

            private async UniTask<MediaLoadResult<TAsset>> WaitForCompletionAsync<TAsset>(
                PendingLoad pendingLoad,
                CancellationToken cancellationToken)
                where TAsset : UnityEngine.Object
            {
                try
                {
                    await pendingLoad.Completion.Task.AttachExternalCancellation(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    CancellationObservedCount++;
                    throw;
                }

                Texture2D texture = new Texture2D(1, 1)
                {
                    name = pendingLoad.Key
                };
                assets.Add(texture);
                return new MediaLoadResult<TAsset>((TAsset)(UnityEngine.Object)texture, new CountingLease());
            }

            private sealed class PendingLoad
            {
                public readonly UniTaskCompletionSource Completion = new UniTaskCompletionSource();

                public PendingLoad(string key)
                {
                    Key = key;
                }

                public string Key { get; }

                public void Complete()
                {
                    Completion.TrySetResult();
                }
            }
        }

        private sealed class CountingLease : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose()
            {
                DisposeCount++;
            }
        }
    }
}
