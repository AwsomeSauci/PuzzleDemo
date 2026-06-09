using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleStartService
    {
        Task<PuzzleStartAttempt> StartNewAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            CancellationToken cancellationToken);
    }
}

