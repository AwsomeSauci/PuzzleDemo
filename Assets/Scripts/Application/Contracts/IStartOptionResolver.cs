using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IStartOptionResolver
    {
        StartOption Resolve(PuzzleDefinition puzzle, int pieceCount);
    }
}

