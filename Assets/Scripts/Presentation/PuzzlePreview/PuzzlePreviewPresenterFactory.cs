using System;
using PuzzleFlow.Application;
using PuzzleFlow.Presentation.Media;
using PuzzleFlow.Presentation.Popups;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewPresenterFactory
    {
        private readonly IPuzzleCatalogQueryService catalogQueryService;
        private readonly IPuzzleMediaQueryService puzzleMediaQueryService;
        private readonly IMediaSpriteService mediaSpriteService;
        private readonly IPuzzleStartStateService startStateService;
        private readonly IPuzzleStartCommandService startCommandService;
        private readonly IPuzzleContinueService continueService;
        private readonly IPuzzleStartOptionViewModelFactory startOptionViewModelFactory;
        private readonly IUniversalPopupService popupService;
        private readonly IPuzzlePopupCatalog popupCatalog;

        public PuzzlePreviewPresenterFactory(
            IPuzzleCatalogQueryService catalogQueryService,
            IPuzzleMediaQueryService puzzleMediaQueryService,
            IMediaSpriteService mediaSpriteService,
            IPuzzleStartStateService startStateService,
            IPuzzleStartCommandService startCommandService,
            IPuzzleContinueService continueService,
            IPuzzleStartOptionViewModelFactory startOptionViewModelFactory,
            IUniversalPopupService popupService,
            IPuzzlePopupCatalog popupCatalog)
        {
            this.catalogQueryService = catalogQueryService ?? throw new ArgumentNullException(nameof(catalogQueryService));
            this.puzzleMediaQueryService = puzzleMediaQueryService ?? throw new ArgumentNullException(nameof(puzzleMediaQueryService));
            this.mediaSpriteService = mediaSpriteService ?? throw new ArgumentNullException(nameof(mediaSpriteService));
            this.startStateService = startStateService ?? throw new ArgumentNullException(nameof(startStateService));
            this.startCommandService = startCommandService ?? throw new ArgumentNullException(nameof(startCommandService));
            this.continueService = continueService ?? throw new ArgumentNullException(nameof(continueService));
            this.startOptionViewModelFactory = startOptionViewModelFactory ?? throw new ArgumentNullException(nameof(startOptionViewModelFactory));
            this.popupService = popupService ?? throw new ArgumentNullException(nameof(popupService));
            this.popupCatalog = popupCatalog;
        }

        public PuzzlePreviewPresenter Create(
            IPuzzlePreviewView view,
            Action<PuzzlePreviewResult> close)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (close == null)
            {
                throw new ArgumentNullException(nameof(close));
            }

            return new PuzzlePreviewPresenter(
                view,
                catalogQueryService,
                puzzleMediaQueryService,
                mediaSpriteService,
                startStateService,
                startCommandService,
                continueService,
                startOptionViewModelFactory,
                popupService,
                popupCatalog,
                close);
        }
    }
}
