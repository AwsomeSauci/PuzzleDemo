using System.Collections.Generic;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleCatalog
    {
        IReadOnlyList<PuzzleDefinition> GetAll();
        PuzzleDefinition Get(PuzzleId puzzleId);
    }
}

