using System.Collections.Generic;

namespace PuzzleFlow.Presentation.Popups
{
    internal static class PopupActionResolver
    {
        private static readonly PopupActionDefinition[] DefaultActions =
        {
            new PopupActionDefinition("ok", "OK", PopupActionRole.Positive)
        };

        public static IReadOnlyList<PopupActionDefinition> Resolve(PopupRequest request)
        {
            if (request?.Actions != null && request.Actions.Count > 0)
            {
                return request.Actions;
            }

            if (!string.IsNullOrWhiteSpace(request?.Message?.ActionLabel))
            {
                return new[]
                {
                    new PopupActionDefinition("ok", request.Message.ActionLabel, PopupActionRole.Positive)
                };
            }

            return DefaultActions;
        }
    }
}
