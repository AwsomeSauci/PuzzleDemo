using System;
using System.Collections.Generic;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleCatalogQueryService : IPuzzleCatalogQueryService
    {
        private readonly IPuzzleCatalog catalog;

        public PuzzleCatalogQueryService(IPuzzleCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public IReadOnlyList<PuzzleDefinition> GetAll()
        {
            return catalog.GetAll();
        }

        public PuzzleDefinition GetById(PuzzleId puzzleId)
        {
            return catalog.Get(puzzleId);
        }
    }
}

