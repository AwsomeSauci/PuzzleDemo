using System.Collections.Generic;

namespace PuzzleFlow.Domain
{
    public sealed class PuzzleDefinition
    {
        public PuzzleDefinition(
            PuzzleId id,
            string title,
            string collectionName,
            IReadOnlyList<PuzzleCutDefinition> cutOptions)
        {
            Id = id;
            Title = title;
            CollectionName = collectionName;
            CutOptions = cutOptions;
        }

        public PuzzleId Id { get; }
        public string Title { get; }
        public string CollectionName { get; }
        public IReadOnlyList<PuzzleCutDefinition> CutOptions { get; }

        public bool TryGetCut(int pieceCount, out PuzzleCutDefinition cut)
        {
            for (int index = 0; index < CutOptions.Count; index++)
            {
                if (CutOptions[index].PieceCount == pieceCount)
                {
                    cut = CutOptions[index];
                    return true;
                }
            }

            cut = null;
            return false;
        }
    }

    public sealed class PuzzleCutDefinition
    {
        public PuzzleCutDefinition(int pieceCount, StartOption startOption)
        {
            PieceCount = pieceCount;
            StartOption = startOption;
        }

        public int PieceCount { get; }
        public StartOption StartOption { get; }
    }

    public sealed class PuzzleProgress
    {
        public PuzzleProgress(PuzzleId puzzleId, int pieceCount, int completedPieces)
        {
            PuzzleId = puzzleId;
            PieceCount = pieceCount;
            CompletedPieces = completedPieces;
        }

        public PuzzleId PuzzleId { get; }
        public int PieceCount { get; }
        public int CompletedPieces { get; }
        public bool CanContinue => CompletedPieces > 0 && CompletedPieces < PieceCount;
    }

    public enum PuzzleStartMode
    {
        Free,
        Coins,
        RewardedAd
    }

    public readonly struct StartOption
    {
        public StartOption(PuzzleStartMode mode, int coinPrice)
        {
            Mode = mode;
            CoinPrice = coinPrice;
        }

        public PuzzleStartMode Mode { get; }
        public int CoinPrice { get; }
    }

    public enum PuzzleStartFailureReason
    {
        None,
        NotEnoughCoins,
        PurchaseUnavailable,
        PurchaseCancelled,
        WeakInternet,
        AdUnavailable,
        AdSkipped
    }

    public sealed class PuzzleStartAttempt
    {
        private PuzzleStartAttempt(bool isSuccess, PuzzleStartFailureReason failureReason)
        {
            IsSuccess = isSuccess;
            FailureReason = failureReason;
        }

        public bool IsSuccess { get; }
        public PuzzleStartFailureReason FailureReason { get; }

        public static PuzzleStartAttempt Success()
        {
            return new PuzzleStartAttempt(true, PuzzleStartFailureReason.None);
        }

        public static PuzzleStartAttempt Failure(PuzzleStartFailureReason reason)
        {
            return new PuzzleStartAttempt(false, reason);
        }
    }
}

