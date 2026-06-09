using System;
using System.Collections.Generic;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Application
{
    public sealed class PuzzleGalleryQueryService : IPuzzleGalleryQueryService
    {
        private readonly IPuzzleCatalogQueryService catalogQueryService;
        private readonly IStartOptionResolver startOptionResolver;
        private readonly IPuzzleGallerySortConfig sortConfig;

        public PuzzleGalleryQueryService(
            IPuzzleCatalogQueryService catalogQueryService,
            IStartOptionResolver startOptionResolver,
            IPuzzleGallerySortConfig sortConfig)
        {
            this.catalogQueryService = catalogQueryService ?? throw new ArgumentNullException(nameof(catalogQueryService));
            this.startOptionResolver = startOptionResolver ?? throw new ArgumentNullException(nameof(startOptionResolver));
            this.sortConfig = sortConfig ?? new DefaultPuzzleGallerySortConfig();
        }

        public IReadOnlyList<PuzzleGalleryItemData> GetItems()
        {
            IReadOnlyList<PuzzleDefinition> puzzles = catalogQueryService.GetAll();
            List<PuzzleGalleryItemData> items = new List<PuzzleGalleryItemData>(puzzles.Count);

            for (int index = 0; index < puzzles.Count; index++)
            {
                PuzzleDefinition puzzle = puzzles[index];
                StartOption primaryStartOption = startOptionResolver.Resolve(
                    puzzle,
                    puzzle.CutOptions[0].PieceCount);
                items.Add(new PuzzleGalleryItemData(puzzle, primaryStartOption));
            }

            items.Sort(CompareItems);
            return items;
        }

        private int CompareItems(PuzzleGalleryItemData left, PuzzleGalleryItemData right)
        {
            int priorityComparison = sortConfig.GetPriority(left.PrimaryStartOption.Mode)
                .CompareTo(sortConfig.GetPriority(right.PrimaryStartOption.Mode));
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            return string.CompareOrdinal(left.Puzzle.Title, right.Puzzle.Title);
        }
    }
}

