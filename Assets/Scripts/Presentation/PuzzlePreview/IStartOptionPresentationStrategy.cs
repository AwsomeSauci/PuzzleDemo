using PuzzleFlow.Domain;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public interface IStartOptionPresentationStrategy
    {
        PuzzleStartMode Mode { get; }
        PuzzleStartButtonViewModel CreateStartButton(StartOption option);
        PuzzleCutOptionViewModel CreateCutOption(PuzzleCutDefinition option, bool isSelected);
    }
}