using NUnit.Framework;
using PuzzleFlow.Presentation.Navigation;
using Cysharp.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace PuzzleFlow.Tests
{
    public sealed class ReusableFragmentRouterTests
    {
        [Test]
        public void ShowAsync_WhenTokenAlreadyCanceled_DoesNotCreateOrOpenFragment()
        {
            GameObject root = new GameObject("RouterTestRoot");
            FakeFragmentFactory factory = new FakeFragmentFactory();
            ReusableFragmentRouter router = new ReusableFragmentRouter(factory, root.transform);
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Task<string> open = router.ShowAsync<Unit, string>(
                new FragmentId("test.fragment"),
                Unit.Value,
                cancellation.Token).AsTask();

            Assert.CatchAsync<System.OperationCanceledException>(async () => await open);
            Assert.That(factory.GetLayerCount, Is.EqualTo(0));
            Assert.That(factory.GetOrCreateCount, Is.EqualTo(0));
            Assert.That(factory.Fragment.OpenCount, Is.EqualTo(0));
            Assert.That(router.IsActive("test.fragment"), Is.False);

            Object.DestroyImmediate(root);
            cancellation.Dispose();
        }

        [Test]
        public async Task ReopeningActiveFragment_CancelingSecondAwait_DoesNotCloseOriginalHandle()
        {
            GameObject root = new GameObject("RouterTestRoot");
            FakeFragmentFactory factory = new FakeFragmentFactory();
            ReusableFragmentRouter router = new ReusableFragmentRouter(factory, root.transform);
            FragmentId fragmentId = new FragmentId("test.fragment");

            Task<string> firstOpen = router.ShowAsync<Unit, string>(
                fragmentId,
                Unit.Value,
                CancellationToken.None).AsTask();

            CancellationTokenSource secondCancellation = new CancellationTokenSource();
            Task<string> secondOpen = router.ShowAsync<Unit, string>(
                fragmentId,
                Unit.Value,
                secondCancellation.Token).AsTask();

            secondCancellation.Cancel();

            Assert.CatchAsync<System.OperationCanceledException>(async () => await secondOpen);
            Assert.That(firstOpen.IsCompleted, Is.False);
            Assert.That(router.IsActive(fragmentId), Is.True);
            Assert.That(factory.Fragment.RebuildCount, Is.EqualTo(1));

            factory.Fragment.Complete("done");

            Assert.That(await firstOpen, Is.EqualTo("done"));
            Object.DestroyImmediate(root);
            secondCancellation.Dispose();
        }

        [Test]
        public void ShowAsync_WhenFragmentIdIsEmpty_ThrowsConfigurationError()
        {
            GameObject root = new GameObject("RouterTestRoot");
            FakeFragmentFactory factory = new FakeFragmentFactory();
            ReusableFragmentRouter router = new ReusableFragmentRouter(factory, root.transform);

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                router.ShowAsync<Unit, string>(FragmentId.Empty, Unit.Value).AsTask());
            Assert.That(factory.GetLayerCount, Is.EqualTo(0));
            Assert.That(factory.GetOrCreateCount, Is.EqualTo(0));
            Assert.That(factory.Fragment.OpenCount, Is.EqualTo(0));

            Object.DestroyImmediate(root);
        }

        [Test]
        public void ShowAsync_WhenFragmentOpenThrows_DoesNotKeepBrokenHandle()
        {
            GameObject root = new GameObject("RouterTestRoot");
            FakeFragmentFactory factory = new FakeFragmentFactory();
            factory.Fragment.ThrowOnOpen = true;
            ReusableFragmentRouter router = new ReusableFragmentRouter(factory, root.transform);
            FragmentId fragmentId = new FragmentId("test.fragment");

            Task<string> open = router.ShowAsync<Unit, string>(
                fragmentId,
                Unit.Value,
                CancellationToken.None).AsTask();

            Assert.ThrowsAsync<System.InvalidOperationException>(async () => await open);
            Assert.That(router.IsActive(fragmentId), Is.False);
            Assert.That(router.TryClose(fragmentId), Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public async Task ClosingFragment_WhenOnCloseThrows_CompletesAwaiter()
        {
            GameObject root = new GameObject("RouterTestRoot");
            GameObject fragmentObject = new GameObject("ThrowingCloseFragment");
            ThrowingCloseFragment fragment = fragmentObject.AddComponent<ThrowingCloseFragment>();
            SingleFragmentFactory factory = new SingleFragmentFactory(fragment);
            ReusableFragmentRouter router = new ReusableFragmentRouter(factory, root.transform);
            FragmentId fragmentId = new FragmentId("test.throwing-close");

            Task<string> open = router.ShowAsync<Unit, string>(
                fragmentId,
                Unit.Value,
                CancellationToken.None).AsTask();

            LogAssert.Expect(LogType.Exception, new Regex("Close failed"));
            fragment.Complete("done");

            Assert.That(await open, Is.EqualTo("done"));
            Assert.That(router.IsActive(fragmentId), Is.False);
            Assert.That(fragment.LifecycleState, Is.EqualTo(FragmentLifecycleState.Disappeared));
            Assert.That(fragment.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(fragmentObject);
        }

        private sealed class FakeFragmentFactory : IFragmentFactory
        {
            public readonly FakeFragment Fragment = new FakeFragment();
            public int GetLayerCount { get; private set; }
            public int GetOrCreateCount { get; private set; }

            public FragmentLayer GetLayer(string fragmentId)
            {
                GetLayerCount++;
                return FragmentLayer.Overlay;
            }

            public FragmentLayer GetLayer(FragmentId fragmentId)
            {
                GetLayerCount++;
                return FragmentLayer.Overlay;
            }

            public IRoutableFragment GetOrCreate(string fragmentId, Transform parent, int sortingOrder)
            {
                GetOrCreateCount++;
                return Fragment;
            }

            public IRoutableFragment GetOrCreate(FragmentId fragmentId, Transform parent, int sortingOrder)
            {
                GetOrCreateCount++;
                return Fragment;
            }
        }

        private sealed class FakeFragment : IRoutableFragment
        {
            private IFragmentController controller;

            public bool ThrowOnOpen { get; set; }
            public int OpenCount { get; private set; }
            public int RebuildCount { get; private set; }

            public void Open(object args, IFragmentController controller)
            {
                OpenCount++;
                if (ThrowOnOpen)
                {
                    throw new System.InvalidOperationException("Open failed.");
                }

                this.controller = controller;
            }

            public void Rebuild(object args)
            {
                RebuildCount++;
            }

            public void Complete(string result)
            {
                controller.Close(result);
            }
        }

        private sealed class SingleFragmentFactory : IFragmentFactory
        {
            private readonly IRoutableFragment fragment;

            public SingleFragmentFactory(IRoutableFragment fragment)
            {
                this.fragment = fragment;
            }

            public FragmentLayer GetLayer(string fragmentId)
            {
                return FragmentLayer.Overlay;
            }

            public FragmentLayer GetLayer(FragmentId fragmentId)
            {
                return FragmentLayer.Overlay;
            }

            public IRoutableFragment GetOrCreate(string fragmentId, Transform parent, int sortingOrder)
            {
                return fragment;
            }

            public IRoutableFragment GetOrCreate(FragmentId fragmentId, Transform parent, int sortingOrder)
            {
                return fragment;
            }
        }

        private sealed class ThrowingCloseFragment : FragmentBase<Unit, string>
        {
            protected override void OnOpen(Unit args)
            {
            }

            protected override void OnClose()
            {
                throw new System.InvalidOperationException("Close failed.");
            }

            public void Complete(string result)
            {
                Close(result);
            }
        }
    }
}
