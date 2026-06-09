using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Media;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PuzzleFlow.Presentation.Gallery
{
    public sealed class GalleryPresenter : IDisposable
    {
        private readonly IPuzzleGalleryQueryService galleryQueryService;
        private readonly IPuzzleMediaQueryService puzzleMediaQueryService;
        private readonly IMediaSpriteService mediaSpriteService;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly SemaphoreSlim previewLoadSemaphore = new SemaphoreSlim(4);
        private readonly HashSet<int> requestedPreviewIndexes = new HashSet<int>();

        private CancellationTokenSource activation;
        private IPuzzleGalleryView activeView;
        private IReadOnlyList<PuzzleGalleryItemData> activeItems = Array.Empty<PuzzleGalleryItemData>();
        private bool isDisposed;

        public GalleryPresenter(
            IPuzzleGalleryQueryService galleryQueryService,
            IPuzzleMediaQueryService puzzleMediaQueryService,
            IMediaSpriteService mediaSpriteService)
        {
            this.galleryQueryService = galleryQueryService ?? throw new ArgumentNullException(nameof(galleryQueryService));
            this.puzzleMediaQueryService = puzzleMediaQueryService ?? throw new ArgumentNullException(nameof(puzzleMediaQueryService));
            this.mediaSpriteService = mediaSpriteService ?? throw new ArgumentNullException(nameof(mediaSpriteService));
        }

        public void Start(IPuzzleGalleryView view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(GalleryPresenter));
            }

            Stop();
            activeView = view;
            activeView.ItemBecameVisible += OnItemBecameVisible;
            activation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            LoadAndRenderAsync(view, activation.Token).Forget();
        }

        public void Stop()
        {
            if (activeView != null)
            {
                activeView.ItemBecameVisible -= OnItemBecameVisible;
            }

            if (activation != null)
            {
                activation.Cancel();
                activation.Dispose();
                activation = null;
            }

            activeView = null;
            activeItems = Array.Empty<PuzzleGalleryItemData>();
            requestedPreviewIndexes.Clear();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            Stop();
            lifetime.Cancel();
            lifetime.Dispose();
            previewLoadSemaphore.Dispose();
        }

        private UniTask LoadAndRenderAsync(IPuzzleGalleryView view, CancellationToken cancellationToken)
        {
            try
            {
                IReadOnlyList<PuzzleGalleryItemData> galleryItems = galleryQueryService.GetItems();
                List<PuzzleGalleryItemViewModel> items = new List<PuzzleGalleryItemViewModel>(galleryItems.Count);
                for (int i = 0; i < galleryItems.Count; i++)
                {
                    PuzzleDefinition puzzle = galleryItems[i].Puzzle;
                    items.Add(new PuzzleGalleryItemViewModel(puzzle, null));
                }

                if (!cancellationToken.IsCancellationRequested && activeView == view)
                {
                    activeItems = galleryItems;
                    view.Render(items);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return UniTask.CompletedTask;
        }

        private void OnItemBecameVisible(int itemIndex)
        {
            CancellationTokenSource currentActivation = activation;
            if (currentActivation == null ||
                activeView == null ||
                itemIndex < 0 ||
                itemIndex >= activeItems.Count ||
                !requestedPreviewIndexes.Add(itemIndex))
            {
                return;
            }

            PuzzleDefinition puzzle = activeItems[itemIndex].Puzzle;
            LoadPreviewAndRenderAsync(activeView, itemIndex, puzzle, currentActivation.Token).Forget();
        }

        private async UniTask LoadPreviewAndRenderAsync(
            IPuzzleGalleryView view,
            int itemIndex,
            PuzzleDefinition puzzle,
            CancellationToken cancellationToken)
        {
            bool semaphoreEntered = false;
            bool previewRendered = false;
            try
            {
                await previewLoadSemaphore.WaitAsync(cancellationToken);
                semaphoreEntered = true;
                try
                {
                    MediaReference previewMedia = puzzleMediaQueryService.GetPreviewMedia(puzzle.Id);
                    Sprite preview = await mediaSpriteService.LoadSpriteAsync(previewMedia, cancellationToken);
                    if (!cancellationToken.IsCancellationRequested && activeView == view)
                    {
                        view.UpdateItem(itemIndex, new PuzzleGalleryItemViewModel(puzzle, preview));
                        previewRendered = true;
                    }
                }
                finally
                {
                    if (semaphoreEntered && !isDisposed)
                    {
                        previewLoadSemaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (!previewRendered && !cancellationToken.IsCancellationRequested && activeView == view)
                {
                    requestedPreviewIndexes.Remove(itemIndex);
                }
            }
        }
    }
}
