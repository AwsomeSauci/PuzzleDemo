using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Presentation.Popups
{
    public sealed class NotificationMessage
    {
        public NotificationMessage(string title, string body, string actionLabel)
        {
            Title = title;
            Body = body;
            ActionLabel = actionLabel;
        }

        public string Title { get; }
        public string Body { get; }
        public string ActionLabel { get; }
    }

    public enum PopupActionRole
    {
        Neutral,
        Positive,
        Negative
    }

    [Serializable]
    public sealed class PopupActionDefinition
    {
        [SerializeField] private string actionId = "ok";
        [SerializeField] private string label = "OK";
        [SerializeField] private PopupActionRole role = PopupActionRole.Positive;

        public PopupActionDefinition()
        {
        }

        public PopupActionDefinition(string actionId, string label, PopupActionRole role)
        {
            this.actionId = string.IsNullOrWhiteSpace(actionId) ? "ok" : actionId;
            this.label = string.IsNullOrWhiteSpace(label) ? "OK" : label;
            this.role = role;
        }

        public string ActionId => string.IsNullOrWhiteSpace(actionId) ? "ok" : actionId;
        public string Label => string.IsNullOrWhiteSpace(label) ? "OK" : label;
        public PopupActionRole Role => role;
        public bool IsPositive => role == PopupActionRole.Positive;

        public void CollectValidationErrors(
            string popupId,
            int index,
            ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                errors.Add($"Popup '{popupId}' action #{index} has empty action id.");
            }

            if (string.IsNullOrWhiteSpace(label))
            {
                errors.Add($"Popup '{popupId}' action #{index} has empty label.");
            }

            if (!Enum.IsDefined(typeof(PopupActionRole), role))
            {
                errors.Add($"Popup '{popupId}' action #{index} has invalid role '{role}'.");
            }
        }
    }

    public sealed class PopupRequest
    {
        public PopupRequest(
            string popupId,
            NotificationMessage message,
            UniversalPopupView prefab,
            IReadOnlyList<PopupActionDefinition> actions)
        {
            PopupId = string.IsNullOrWhiteSpace(popupId) ? string.Empty : popupId.Trim();
            Message = message;
            Prefab = prefab;
            Actions = actions;
        }

        public string PopupId { get; }
        public NotificationMessage Message { get; }
        public UniversalPopupView Prefab { get; }
        public IReadOnlyList<PopupActionDefinition> Actions { get; }
    }

    public sealed class PopupResult
    {
        private PopupResult(string popupId, string actionId, PopupActionRole role, bool isDismissed)
        {
            PopupId = popupId ?? string.Empty;
            ActionId = actionId ?? string.Empty;
            Role = role;
            IsDismissed = isDismissed;
        }

        public string PopupId { get; }
        public string ActionId { get; }
        public PopupActionRole Role { get; }
        public bool IsDismissed { get; }
        public bool IsPositive => !IsDismissed && Role == PopupActionRole.Positive;
        public bool IsNegative => !IsDismissed && Role == PopupActionRole.Negative;

        public static PopupResult FromAction(string popupId, PopupActionDefinition action)
        {
            return new PopupResult(
                popupId,
                action?.ActionId,
                action != null ? action.Role : PopupActionRole.Neutral,
                false);
        }

        public static PopupResult Dismissed(string popupId)
        {
            return new PopupResult(popupId, string.Empty, PopupActionRole.Neutral, true);
        }
    }
}
