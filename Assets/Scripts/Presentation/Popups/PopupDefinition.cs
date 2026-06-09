using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Presentation.Popups
{
    [CreateAssetMenu(menuName = "Puzzle Flow/Popup Definition", fileName = "PopupDefinition")]
    public sealed class PopupDefinition : ScriptableObject
    {
        private static readonly PopupActionDefinition[] DefaultActions =
        {
            new PopupActionDefinition("ok", "OK", PopupActionRole.Positive)
        };

        [SerializeField] private string popupId;
        [SerializeField] private string title;
        [SerializeField] private string body;
        [SerializeField] private string actionLabel = "OK";
        [SerializeField] private UniversalPopupView popupPrefab;
        [SerializeField] private PopupActionDefinition[] actions;

        public string PopupId => string.IsNullOrWhiteSpace(popupId) ? name : popupId.Trim();
        public string Title => title;
        public string Body => body;
        public string ActionLabel => actionLabel;
        public UniversalPopupView PopupPrefab => popupPrefab;
        public IReadOnlyList<PopupActionDefinition> Actions => ResolveActions();

        public NotificationMessage Build(params object[] args)
        {
            string resolvedBody = body;
            if (args != null && args.Length > 0 && !string.IsNullOrEmpty(body))
            {
                try
                {
                    resolvedBody = string.Format(body, args);
                }
                catch (FormatException exception)
                {
                    throw new InvalidOperationException(
                        $"Popup definition '{PopupId}' has invalid body format.",
                        exception);
                }
            }

            return new NotificationMessage(title, resolvedBody, actionLabel);
        }

        public PopupRequest BuildRequest(params object[] args)
        {
            ValidateOrThrow();

            if (popupPrefab == null)
            {
                throw new InvalidOperationException($"Popup definition '{PopupId}' has no popup prefab.");
            }

            return new PopupRequest(PopupId, Build(args), popupPrefab, ResolveActions());
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
                $"{nameof(PopupDefinition)} '{name}' is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }

        internal void CollectValidationErrors(ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(popupId))
            {
                errors.Add("Popup id is empty. Use a stable popup GUID/key instead of relying on the asset name.");
            }

            if (popupPrefab == null)
            {
                errors.Add($"Popup '{PopupId}' has no popup prefab.");
            }

            if (actions == null || actions.Length == 0)
            {
                return;
            }

            HashSet<string> actionIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < actions.Length; index++)
            {
                PopupActionDefinition action = actions[index];
                if (action == null)
                {
                    errors.Add($"Popup '{PopupId}' has null action at index {index}.");
                    continue;
                }

                action.CollectValidationErrors(PopupId, index, errors);
                if (!actionIds.Add(action.ActionId))
                {
                    errors.Add($"Popup '{PopupId}' has duplicate action id '{action.ActionId}'.");
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError($"{nameof(PopupDefinition)} '{name}': {errors[index]}", this);
            }
        }
#endif

        private IReadOnlyList<PopupActionDefinition> ResolveActions()
        {
            if (actions == null || actions.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(actionLabel) && actionLabel != DefaultActions[0].Label)
                {
                    return new[]
                    {
                        new PopupActionDefinition("ok", actionLabel, PopupActionRole.Positive)
                    };
                }

                return DefaultActions;
            }

            return actions;
        }
    }
}
