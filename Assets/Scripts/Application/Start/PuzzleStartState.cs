using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleStartState
    {
        public PuzzleStartState(
            PuzzleDefinition puzzle,
            int selectedPieceCount,
            StartOption startOption,
            PuzzleProgress progress,
            int walletBalance)
        {
            Puzzle = puzzle;
            SelectedPieceCount = selectedPieceCount;
            StartOption = startOption;
            Progress = progress;
            WalletBalance = walletBalance;
        }

        public PuzzleDefinition Puzzle { get; }
        public int SelectedPieceCount { get; }
        public StartOption StartOption { get; }
        public PuzzleProgress Progress { get; }
        public int WalletBalance { get; }
        public bool CanContinue => Progress != null && Progress.CanContinue;
    }
}

