using System;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Infrastructure
{
    [Serializable]
    public sealed class SerializedPuzzleProgressSeed
    {
        [SerializeField] private string puzzleId;
        [SerializeField] private int pieceCount;
        [SerializeField] private int completedPieces;

        public PuzzleProgress ToProgress()
        {
            return new PuzzleProgress(new PuzzleId(puzzleId), pieceCount, completedPieces);
        }
    }
}
