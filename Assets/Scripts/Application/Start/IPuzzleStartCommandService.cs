using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleStartCommandService
    {
        Task<PuzzleStartAttempt> StartNewAsync(
            PuzzleId puzzleId,
            int pieceCount,
            CancellationToken cancellationToken);
    }
}

