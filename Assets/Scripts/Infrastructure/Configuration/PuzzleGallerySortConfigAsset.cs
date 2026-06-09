using PuzzleFlow.Application;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Infrastructure
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Gallery Sort Config", fileName = "PuzzleGallerySortConfig")]
    public sealed class PuzzleGallerySortConfigAsset : ScriptableObject, IPuzzleGallerySortConfig
    {
        [SerializeField] private PuzzleStartMode[] modeOrder =
        {
            PuzzleStartMode.Free,
            PuzzleStartMode.Coins,
            PuzzleStartMode.RewardedAd
        };

        public int GetPriority(PuzzleStartMode mode)
        {
            if (modeOrder == null || modeOrder.Length == 0)
            {
                return new DefaultPuzzleGallerySortConfig().GetPriority(mode);
            }

            for (int index = 0; index < modeOrder.Length; index++)
            {
                if (modeOrder[index] == mode)
                {
                    return index;
                }
            }

            return int.MaxValue;
        }
    }
}

