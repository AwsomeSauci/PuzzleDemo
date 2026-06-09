using System;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public interface IPuzzlePreviewView
    {
        event Action<int> PieceCountSelected;
        event Action StartClicked;
        event Action ContinueClicked;
        event Action CloseClicked;

        void Render(PuzzlePreviewViewModel viewModel);
        void SetBusy(bool isBusy);
    }
}

