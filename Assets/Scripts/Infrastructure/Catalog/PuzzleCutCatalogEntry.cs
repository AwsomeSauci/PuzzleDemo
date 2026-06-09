using System;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Infrastructure
{
    [Serializable]
    public sealed class PuzzleCutCatalogEntry
    {
        [SerializeField] private int pieceCount;
        [SerializeField] private PuzzleStartMode startMode;
        [SerializeField] private int coinPrice;

        public PuzzleCutDefinition ToDefinition(string puzzleId)
        {
            if (pieceCount <= 0)
            {
                throw new InvalidOperationException($"Puzzle '{puzzleId}' has invalid cut with {pieceCount} pieces.");
            }

            int normalizedPrice = startMode == PuzzleStartMode.Coins ? coinPrice : 0;
            if (startMode == PuzzleStartMode.Coins && normalizedPrice <= 0)
            {
                throw new InvalidOperationException($"Puzzle '{puzzleId}' has coin cut with invalid price.");
            }

            return new PuzzleCutDefinition(
                pieceCount,
                new StartOption(startMode, normalizedPrice));
        }
    }
}
