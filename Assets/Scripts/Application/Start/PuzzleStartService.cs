using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleStartService : IPuzzleStartService
    {
        private readonly IStartOptionResolver startOptionResolver;
        private readonly Dictionary<PuzzleStartMode, IPuzzleStartModeHandler> handlersByMode =
            new Dictionary<PuzzleStartMode, IPuzzleStartModeHandler>();

        public PuzzleStartService(
            IStartOptionResolver startOptionResolver,
            IEnumerable<IPuzzleStartModeHandler> handlers)
        {
            this.startOptionResolver = startOptionResolver ?? throw new ArgumentNullException(nameof(startOptionResolver));
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }

            foreach (IPuzzleStartModeHandler handler in handlers)
            {
                if (handler == null)
                {
                    throw new ArgumentException("Start mode handler collection contains null.", nameof(handlers));
                }

                if (handlersByMode.ContainsKey(handler.Mode))
                {
                    throw new InvalidOperationException($"Start mode handler for '{handler.Mode}' is registered twice.");
                }

                handlersByMode[handler.Mode] = handler;
            }
        }

        public async Task<PuzzleStartAttempt> StartNewAsync(
            PuzzleDefinition puzzle,
            int pieceCount,
            CancellationToken cancellationToken)
        {
            if (puzzle == null)
            {
                throw new ArgumentNullException(nameof(puzzle));
            }

            cancellationToken.ThrowIfCancellationRequested();

            StartOption startOption = startOptionResolver.Resolve(puzzle, pieceCount);
            if (!handlersByMode.TryGetValue(startOption.Mode, out IPuzzleStartModeHandler handler))
            {
                throw new InvalidOperationException($"Start mode handler for '{startOption.Mode}' is not registered.");
            }

            return await handler.StartAsync(puzzle, pieceCount, startOption, cancellationToken);
        }
    }
}
