using System.Collections.Generic;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    public sealed class PuzzlePreviewViewModel
    {
        public PuzzlePreviewViewModel(
            PuzzleDefinition puzzle,
            IReadOnlyList<PuzzleCutOptionViewModel> cutOptions,
            Sprite preview,
            PuzzleStartButtonViewModel startButton,
            PuzzleProgress progress,
            int walletBalance)
        {
            Puzzle = puzzle;
            CutOptions = cutOptions;
            Preview = preview;
            StartButton = startButton;
            Progress = progress;
            WalletBalance = walletBalance;
        }

        public PuzzleDefinition Puzzle { get; }
        public IReadOnlyList<PuzzleCutOptionViewModel> CutOptions { get; }
        public Sprite Preview { get; }
        public PuzzleStartButtonViewModel StartButton { get; }
        public PuzzleProgress Progress { get; }
        public int WalletBalance { get; }
        public bool CanContinue => Progress != null && Progress.CanContinue;
    }
}

