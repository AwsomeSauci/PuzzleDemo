using PuzzleFlow.Presentation.Navigation;
using PuzzleFlow.Presentation.Popups;
using PuzzleFlow.Presentation.PuzzlePreview;

namespace PuzzleFlow.Presentation
{
    public static class PuzzleFlowRoutes
    {
        public static readonly FragmentRoute<Unit, Unit> Gallery =
            new FragmentRoute<Unit, Unit>(FragmentIds.Gallery);

        public static readonly FragmentRoute<PuzzlePreviewArgs, PuzzlePreviewResult> PuzzlePreview =
            new FragmentRoute<PuzzlePreviewArgs, PuzzlePreviewResult>(FragmentIds.PuzzlePreview);

        public static readonly FragmentRoute<PopupRequest, PopupResult> UniversalPopup =
            new FragmentRoute<PopupRequest, PopupResult>(FragmentIds.UniversalPopup);
    }
}
