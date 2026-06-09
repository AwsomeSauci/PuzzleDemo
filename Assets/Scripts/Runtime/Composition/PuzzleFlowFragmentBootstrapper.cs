using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Presentation;
using PuzzleFlow.Presentation.Navigation;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Runtime
{
    public sealed class PuzzleFlowFragmentBootstrapper : IInitializable, IDisposable
    {
        private readonly ReusableFragmentRouter fragmentRouter;
        private readonly bool showGalleryOnStart;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();

        public PuzzleFlowFragmentBootstrapper(
            ReusableFragmentRouter fragmentRouter,
            bool showGalleryOnStart)
        {
            this.fragmentRouter = fragmentRouter;
            this.showGalleryOnStart = showGalleryOnStart;
        }

        public void Initialize()
        {
            fragmentRouter.PreloadAll();
            if (showGalleryOnStart)
            {
                ShowGalleryAsync(lifetime.Token).Forget();
            }
        }

        public void Dispose()
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private async UniTask ShowGalleryAsync(CancellationToken cancellationToken)
        {
            try
            {
                await fragmentRouter.ShowAsync(PuzzleFlowRoutes.Gallery, Unit.Value, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
