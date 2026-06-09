using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class DefaultPuzzleGallerySortConfig : IPuzzleGallerySortConfig
    {
        public int GetPriority(PuzzleStartMode mode)
        {
            switch (mode)
            {
                case PuzzleStartMode.Free:
                    return 0;
                case PuzzleStartMode.Coins:
                    return 1;
                case PuzzleStartMode.RewardedAd:
                    return 2;
                default:
                    return int.MaxValue;
            }
        }
    }
}

