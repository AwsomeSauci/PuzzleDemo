using System.Collections.Generic;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleCatalogQueryService
    {
        IReadOnlyList<PuzzleDefinition> GetAll();
        PuzzleDefinition GetById(PuzzleId puzzleId);
    }
}
