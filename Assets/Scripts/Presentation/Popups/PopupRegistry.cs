using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Presentation.Popups
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Popup Registry", fileName = "PopupRegistry")]
    public sealed class PopupRegistry : ScriptableObject
    {
        [SerializeField] private PopupDefinition[] definitions;

        public bool TryGet(string popupId, out PopupDefinition definition)
        {
            if (definitions != null)
            {
                for (int index = 0; index < definitions.Length; index++)
                {
                    PopupDefinition current = definitions[index];
                    if (current != null && string.Equals(current.PopupId, popupId, StringComparison.Ordinal))
                    {
                        definition = current;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public IReadOnlyList<string> GetValidationErrors()
        {
            List<string> errors = new List<string>();
            CollectValidationErrors(errors);
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
                $"{nameof(PopupRegistry)} '{name}' is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }

        private void CollectValidationErrors(List<string> errors)
        {
            if (definitions == null || definitions.Length == 0)
            {
                errors.Add("Popup registry has no definitions.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < definitions.Length; index++)
            {
                PopupDefinition definition = definitions[index];
                if (definition == null)
                {
                    errors.Add($"Definition #{index} is null.");
                    continue;
                }

                definition.CollectValidationErrors(errors);
                string id = definition.PopupId;
                if (!string.IsNullOrEmpty(id) && !ids.Add(id))
                {
                    errors.Add($"Definition #{index} duplicates popup id '{id}'.");
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError($"{nameof(PopupRegistry)} '{name}': {errors[index]}", this);
            }
        }
#endif
    }
}
