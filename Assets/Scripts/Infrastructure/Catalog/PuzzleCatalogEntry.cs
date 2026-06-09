using System;
using System.Collections.Generic;
using PuzzleFlow.Domain;
using UnityEngine;

namespace PuzzleFlow.Infrastructure
{
    [Serializable]
    public sealed class PuzzleCatalogEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private string collectionName;
        [SerializeField] private PuzzleCutCatalogEntry[] cutOptions;
        [SerializeField] private MediaReference previewMedia;

        public string Id => id;
        public MediaReference PreviewMedia => previewMedia;

        public PuzzleDefinition ToDefinition()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Puzzle catalog entry has empty id.");
            }

            IReadOnlyList<PuzzleCutDefinition> cuts = BuildCutDefinitions();
            if (cuts.Count == 0)
            {
                throw new InvalidOperationException($"Puzzle '{id}' has no cut options.");
            }

            return new PuzzleDefinition(
                new PuzzleId(id),
                title,
                collectionName,
                cuts);
        }

        private IReadOnlyList<PuzzleCutDefinition> BuildCutDefinitions()
        {
            if (cutOptions == null || cutOptions.Length == 0)
            {
                return Array.Empty<PuzzleCutDefinition>();
            }

            List<PuzzleCutDefinition> cuts = new List<PuzzleCutDefinition>(cutOptions.Length);
            for (int index = 0; index < cutOptions.Length; index++)
            {
                if (cutOptions[index] == null)
                {
                    throw new InvalidOperationException($"Puzzle '{id}' has null cut option at index {index}.");
                }

                cuts.Add(cutOptions[index].ToDefinition(id));
            }

            return cuts;
        }
    }
}
