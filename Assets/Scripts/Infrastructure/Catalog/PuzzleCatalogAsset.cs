using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Infrastructure
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Puzzle Catalog", fileName = "PuzzleCatalog")]
    public sealed class PuzzleCatalogAsset : ScriptableObject
    {
        [SerializeField] private PuzzleCatalogEntry[] entries;

        public IReadOnlyList<PuzzleCatalogEntry> Entries => entries ?? Array.Empty<PuzzleCatalogEntry>();
    }
}
