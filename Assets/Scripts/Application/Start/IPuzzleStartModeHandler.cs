using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public interface IPuzzleStartModeHandler
    {
        PuzzleStartMode Mode { get; }

        Task<PuzzleStartAttempt> StartAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            StartOption startOption,
            CancellationToken cancellationToken);
    }
}

