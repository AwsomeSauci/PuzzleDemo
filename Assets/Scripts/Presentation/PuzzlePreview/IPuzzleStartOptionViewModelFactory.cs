using PuzzleFlow.Domain;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public interface IPuzzleStartOptionViewModelFactory
    {
        PuzzleStartButtonViewModel CreateStartButton(StartOption option);
        PuzzleCutOptionViewModel CreateCutOption(PuzzleCutDefinition option, bool isSelected);
    }
}