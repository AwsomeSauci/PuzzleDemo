using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Navigation;
using PuzzleFlow.Presentation.Popups;
using PuzzleFlow.Presentation.PuzzlePreview;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Presentation.Gallery
{
    public sealed class GalleryFragment : FragmentBase<Unit, Unit>
    {
        [SerializeField] private GalleryImageListView imageList;

        private GalleryPresenter presenter;
        private IPuzzleCatalogQueryService catalogQueryService;
        private IFragmentRouter fragmentRouter;
        private IUniversalPopupService popupService;
        private IPuzzlePopupCatalog popupCatalog;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private CancellationTokenSource activation;
        private bool isOpeningPreview;

        [Inject]
        public void Construct(
            GalleryPresenter presenter,
            IPuzzleCatalogQueryService catalogQueryService,
            IFragmentRouter fragmentRouter,
            IUniversalPopupService popupService,
            IPuzzlePopupCatalog popupCatalog)
        {
            this.presenter = presenter;
            this.catalogQueryService = catalogQueryService;
            this.fragmentRouter = fragmentRouter;
            this.popupService = popupService;
            this.popupCatalog = popupCatalog;
        }

        protected override void OnOpen(Unit args)
        {
        }

        protected override void OnAppeared()
        {
            base.OnAppeared();
            StopActivation();
            activation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            imageList.Initialize();
            imageList.ItemClicked.RemoveListener(OnPuzzleSelected);
            imageList.ItemClicked.AddListener(OnPuzzleSelected);
            presenter.Start(imageList);
        }

        protected override void OnClose()
        {
            imageList.ItemClicked.RemoveListener(OnPuzzleSelected);
            StopActivation();
            presenter?.Stop();
            imageList.Clear();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            StopActivation();
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private void OnPuzzleSelected(PuzzleId puzzleId)
        {
            CancellationTokenSource currentActivation = activation;
            if (currentActivation == null)
            {
                return;
            }

            OpenPuzzlePreviewFragmentAsync(puzzleId, currentActivation, currentActivation.Token).Forget();
        }

        private async UniTask OpenPuzzlePreviewFragmentAsync(
            PuzzleId puzzleId,
            CancellationTokenSource source,
            CancellationToken cancellationToken)
        {
            if (isOpeningPreview)
            {
                return;
            }

            isOpeningPreview = true;
            try
            {
                PuzzlePreviewArgs args = new PuzzlePreviewArgs(puzzleId);
                PuzzlePreviewResult result = await fragmentRouter.ShowAsync(
                    PuzzleFlowRoutes.PuzzlePreview,
                    args,
                    cancellationToken);

                if (result == null || result.Action == PuzzlePreviewAction.Cancelled)
                {
                    return;
                }

                PuzzleDefinition puzzle = catalogQueryService.GetById(result.PuzzleId);
                if (popupCatalog != null)
                {
                    await popupService.ShowAsync(popupCatalog.BuildCompletion(result, puzzle), cancellationToken);
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
                if (activation == source)
                {
                    isOpeningPreview = false;
                }
            }
        }

        private void StopActivation()
        {
            if (activation == null)
            {
                return;
            }

            activation.Cancel();
            activation.Dispose();
            activation = null;
            isOpeningPreview = false;
        }
    }
}
