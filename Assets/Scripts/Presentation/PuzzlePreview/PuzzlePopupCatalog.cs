using PuzzleFlow.Domain;
using PuzzleFlow.Presentation.Popups;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Puzzle Popup Catalog", fileName = "PuzzlePopupCatalog")]
    public sealed class PuzzlePopupCatalog : ScriptableObject, IPuzzlePopupCatalog
    {
        [SerializeField] private PopupDefinition started;
        [SerializeField] private PopupDefinition continued;
        [SerializeField] private PopupDefinition notEnoughCoins;
        [SerializeField] private PopupDefinition purchaseUnavailable;
        [SerializeField] private PopupDefinition purchaseCancelled;
        [SerializeField] private PopupDefinition weakInternet;
        [SerializeField] private PopupDefinition adSkipped;
        [SerializeField] private PopupDefinition adUnavailable;

        public PopupRequest BuildCompletion(PuzzlePreviewResult result, PuzzleDefinition puzzle)
        {
            PopupDefinition definition = result.Action == PuzzlePreviewAction.Continued ? continued : started;
            return definition != null
                ? definition.BuildRequest(result.PieceCount, puzzle.Title)
                : throw new InvalidOperationException($"Popup definition for '{result.Action}' is not configured.");
        }

        public PopupRequest BuildFailure(PuzzleStartFailureReason reason)
        {
            PopupDefinition definition = GetFailureDefinition(reason);
            return definition != null
                ? definition.BuildRequest()
                : throw new InvalidOperationException($"Popup definition for '{reason}' is not configured.");
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new List<string>();
            ValidateDefinition(nameof(started), started, errors);
            ValidateDefinition(nameof(continued), continued, errors);
            ValidateDefinition(nameof(notEnoughCoins), notEnoughCoins, errors);
            ValidateDefinition(nameof(purchaseUnavailable), purchaseUnavailable, errors);
            ValidateDefinition(nameof(purchaseCancelled), purchaseCancelled, errors);
            ValidateDefinition(nameof(weakInternet), weakInternet, errors);
            ValidateDefinition(nameof(adSkipped), adSkipped, errors);
            ValidateDefinition(nameof(adUnavailable), adUnavailable, errors);
            return errors;
        }

        public void ValidateOrThrow()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            if (errors.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{nameof(PuzzlePopupCatalog)} '{name}' is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }

        private PopupDefinition GetFailureDefinition(PuzzleStartFailureReason reason)
        {
            switch (reason)
            {
                case PuzzleStartFailureReason.NotEnoughCoins:
                    return notEnoughCoins;
                case PuzzleStartFailureReason.PurchaseUnavailable:
                    return purchaseUnavailable != null ? purchaseUnavailable : adUnavailable;
                case PuzzleStartFailureReason.PurchaseCancelled:
                    return purchaseCancelled != null ? purchaseCancelled : adUnavailable;
                case PuzzleStartFailureReason.WeakInternet:
                    return weakInternet;
                case PuzzleStartFailureReason.AdSkipped:
                    return adSkipped;
                default:
                    return adUnavailable;
            }
        }

        private static void ValidateDefinition(
            string fieldName,
            PopupDefinition definition,
            ICollection<string> errors)
        {
            if (definition == null)
            {
                errors.Add($"Popup catalog field '{fieldName}' is not configured.");
                return;
            }

            definition.CollectValidationErrors(errors);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError($"{nameof(PuzzlePopupCatalog)} '{name}': {errors[index]}", this);
            }
        }
#endif
    }
}
