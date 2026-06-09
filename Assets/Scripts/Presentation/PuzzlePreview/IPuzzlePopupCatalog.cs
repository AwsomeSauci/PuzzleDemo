using PuzzleFlow.Presentation.Popups;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public interface IPuzzlePopupCatalog
    {
        PopupRequest BuildCompletion(PuzzlePreviewResult result, PuzzleDefinition puzzle);
        PopupRequest BuildFailure(PuzzleStartFailureReason reason);
    }
}


