using PuzzleFlow.Presentation.Popups;
using PuzzleFlow.Presentation.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewPresenter : IDisposable
    {
        private readonly IPuzzlePreviewView view;
        private readonly IPuzzleCatalogQueryService catalogQueryService;
        private readonly IPuzzleMediaQueryService puzzleMediaQueryService;
        private readonly IMediaSpriteService mediaSpriteService;
        private readonly IPuzzleStartStateService startStateService;
        private readonly IPuzzleStartCommandService startCommandService;
        private readonly IPuzzleContinueService continueService;
        private readonly IPuzzleStartOptionViewModelFactory startOptionViewModelFactory;
        private readonly IUniversalPopupService popupService;
        private readonly IPuzzlePopupCatalog popupCatalog;
        private readonly Action<PuzzlePreviewResult> close;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private CancellationTokenSource activation;
        private CancellationTokenSource render;

        private PuzzleDefinition puzzle;
        private int selectedPieceCount;
        private bool isBusy;
        private int renderVersion;

        public PuzzlePreviewPresenter(
            IPuzzlePreviewView view,
            IPuzzleCatalogQueryService catalogQueryService,
            IPuzzleMediaQueryService puzzleMediaQueryService,
            IMediaSpriteService mediaSpriteService,
            IPuzzleStartStateService startStateService,
            IPuzzleStartCommandService startCommandService,
            IPuzzleContinueService continueService,
            IPuzzleStartOptionViewModelFactory startOptionViewModelFactory,
            IUniversalPopupService popupService,
            IPuzzlePopupCatalog popupCatalog,
            Action<PuzzlePreviewResult> close)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.catalogQueryService = catalogQueryService ?? throw new ArgumentNullException(nameof(catalogQueryService));
            this.puzzleMediaQueryService = puzzleMediaQueryService ?? throw new ArgumentNullException(nameof(puzzleMediaQueryService));
            this.mediaSpriteService = mediaSpriteService ?? throw new ArgumentNullException(nameof(mediaSpriteService));
            this.startStateService = startStateService ?? throw new ArgumentNullException(nameof(startStateService));
            this.startCommandService = startCommandService ?? throw new ArgumentNullException(nameof(startCommandService));
            this.continueService = continueService ?? throw new ArgumentNullException(nameof(continueService));
            this.startOptionViewModelFactory = startOptionViewModelFactory ?? throw new ArgumentNullException(nameof(startOptionViewModelFactory));
            this.popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
            this.popupCatalog = popupCatalog;
            this.close = close ?? throw new ArgumentNullException(nameof(close));

            this.view.PieceCountSelected += OnPieceCountSelected;
            this.view.StartClicked += OnStartClicked;
            this.view.ContinueClicked += OnContinueClicked;
            this.view.CloseClicked += OnCloseClicked;
        }

        public void Start(PuzzlePreviewArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Stop();
            activation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            puzzle = catalogQueryService.GetById(args.PuzzleId);
            selectedPieceCount = startStateService.SelectInitialPieceCount(args.PuzzleId);
            Render();
        }

        public void Stop()
        {
            if (activation == null)
            {
                return;
            }

            CancelRender();
            activation.Cancel();
            activation.Dispose();
            activation = null;
            isBusy = false;
        }

        public void Dispose()
        {
            view.PieceCountSelected -= OnPieceCountSelected;
            view.StartClicked -= OnStartClicked;
            view.ContinueClicked -= OnContinueClicked;
            view.CloseClicked -= OnCloseClicked;
            Stop();
            lifetime.Cancel();
            lifetime.Dispose();
        }

        private void OnPieceCountSelected(int pieceCount)
        {
            if (isBusy)
            {
                return;
            }

            selectedPieceCount = pieceCount;
            Render();
        }

        private void OnStartClicked()
        {
            if (isBusy)
            {
                return;
            }

            StartNewPuzzleWithLoggingAsync(GetActiveToken()).Forget();
        }

        private async UniTask StartNewPuzzleWithLoggingAsync(CancellationToken cancellationToken)
        {
            try
            {
                await StartNewPuzzleAsync(cancellationToken);
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
        }

        private async UniTask StartNewPuzzleAsync(CancellationToken cancellationToken)
        {
            CancelRender();
            renderVersion++;
            isBusy = true;
            view.SetBusy(true);

            PuzzleStartAttempt attempt;
            try
            {
                attempt = await startCommandService.StartNewAsync(puzzle.Id, selectedPieceCount, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                isBusy = false;
                if (!lifetime.IsCancellationRequested && activation != null)
                {
                    view.SetBusy(false);
                }
            }

            if (attempt.IsSuccess)
            {
                close.Invoke(new PuzzlePreviewResult(PuzzlePreviewAction.Started, puzzle.Id, selectedPieceCount));
                return;
            }

            await ShowFailureAsync(attempt.FailureReason, cancellationToken);
            if (!lifetime.IsCancellationRequested && activation != null)
            {
                Render();
            }
        }

        private void OnContinueClicked()
        {
            if (!continueService.CanContinue(puzzle.Id, selectedPieceCount))
            {
                Render();
                return;
            }

            close.Invoke(new PuzzlePreviewResult(PuzzlePreviewAction.Continued, puzzle.Id, selectedPieceCount));
        }

        private void OnCloseClicked()
        {
            close.Invoke(new PuzzlePreviewResult(PuzzlePreviewAction.Cancelled, puzzle.Id, selectedPieceCount));
        }

        private async UniTask ShowFailureAsync(PuzzleStartFailureReason reason, CancellationToken cancellationToken)
        {
            if (popupCatalog == null)
            {
                return;
            }

            await popupService.ShowAsync(popupCatalog.BuildFailure(reason), cancellationToken);
        }

        private void Render()
        {
            CancelRender();
            if (activation == null || lifetime.IsCancellationRequested)
            {
                return;
            }

            render = CancellationTokenSource.CreateLinkedTokenSource(activation.Token);
            RenderAsync(++renderVersion, render.Token).Forget();
        }

        private async UniTask RenderAsync(int version, CancellationToken cancellationToken)
        {
            PuzzleStartState state = startStateService.GetState(puzzle.Id, selectedPieceCount);
            try
            {
                MediaReference previewMedia = puzzleMediaQueryService.GetPreviewMedia(state.Puzzle.Id);
                Sprite preview = await mediaSpriteService.LoadSpriteAsync(previewMedia, cancellationToken);
                if (cancellationToken.IsCancellationRequested || version != renderVersion || isBusy)
                {
                    return;
                }

                view.Render(new PuzzlePreviewViewModel(
                    state.Puzzle,
                    BuildCutOptions(state.Puzzle.CutOptions, state.SelectedPieceCount),
                    preview,
                    startOptionViewModelFactory.CreateStartButton(state.StartOption),
                    state.Progress,
                    state.WalletBalance));
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
        }

        private IReadOnlyList<PuzzleCutOptionViewModel> BuildCutOptions(
            IReadOnlyList<PuzzleCutDefinition> cutOptions,
            int selectedPieceCount)
        {
            PuzzleCutOptionViewModel[] result = new PuzzleCutOptionViewModel[cutOptions.Count];
            for (int index = 0; index < cutOptions.Count; index++)
            {
                PuzzleCutDefinition option = cutOptions[index];
                result[index] = startOptionViewModelFactory.CreateCutOption(
                    option,
                    option.PieceCount == selectedPieceCount);
            }

            return result;
        }

        private void CancelRender()
        {
            if (render == null)
            {
                return;
            }

            render.Cancel();
            render.Dispose();
            render = null;
        }

        private CancellationToken GetActiveToken()
        {
            return activation?.Token ?? lifetime.Token;
        }
    }
}



