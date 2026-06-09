using System;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleMediaQueryService : IPuzzleMediaQueryService
    {
        private readonly IPuzzleMediaCatalog mediaCatalog;

        public PuzzleMediaQueryService(IPuzzleMediaCatalog mediaCatalog)
        {
            this.mediaCatalog = mediaCatalog ?? throw new ArgumentNullException(nameof(mediaCatalog));
        }

        public MediaReference GetPreviewMedia(PuzzleId puzzleId)
        {
            return mediaCatalog.GetPreviewMedia(puzzleId);
        }
    }
}

