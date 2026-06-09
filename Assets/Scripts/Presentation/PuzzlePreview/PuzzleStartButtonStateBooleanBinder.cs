using PuzzleFlow.Presentation.State;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuzzleFlow.Presentation.PuzzlePreview
{
    [DisallowMultipleComponent]
    public sealed class PuzzleStartButtonStateBooleanBinder :
        BooleanEnumViewStateBinder<PuzzleStartButtonState>
    {
        [SerializeField] private PuzzlePreviewFragmentView source;
        [SerializeField] private PuzzleStartButtonStateBinding[] bindings;

        protected override IReadOnlyList<BooleanEnumStateBinding<PuzzleStartButtonState>> Bindings => bindings;

        private void Awake()
        {
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            if (source == null)
            {
                Debug.LogError($"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has no state source.", this);
                return;
            }

            source.StateChanged += OnStateChanged;
            source.StartButtonChanged += OnStartButtonChanged;
            Apply(source.CurrentState);
        }

        private void OnDisable()
        {
            if (source != null)
            {
                source.StateChanged -= OnStateChanged;
                source.StartButtonChanged -= OnStartButtonChanged;
            }
        }

        private void OnStateChanged(PuzzleStartButtonState state)
        {
            Apply(state);
        }

        private void OnStartButtonChanged(PuzzleStartButtonViewModel _)
        {
            if (source != null)
            {
                Apply(source.CurrentState);
            }
        }

        private void Apply(PuzzleStartButtonState state)
        {
            if (bindings == null)
            {
                return;
            }

            PuzzleStartButtonViewModel startButton = source != null
                ? source.CurrentStartButton
                : PuzzleStartButtonViewModel.None;

            for (int index = 0; index < bindings.Length; index++)
            {
                bindings[index]?.Apply(state, startButton);
            }
        }

        private void ValidateConfiguration()
        {
            if (source == null)
            {
                Debug.LogError($"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has no state source.", this);
            }

            if (bindings == null || bindings.Length == 0)
            {
                Debug.LogWarning($"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has no bindings.", this);
                return;
            }

            HashSet<PuzzleStartButtonState> configuredStates = new HashSet<PuzzleStartButtonState>();
            for (int index = 0; index < bindings.Length; index++)
            {
                if (bindings[index] == null)
                {
                    Debug.LogWarning($"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has null binding at index {index}.", this);
                    continue;
                }

                bindings[index].Validate(this, name, index);
                if (!configuredStates.Add(bindings[index].State))
                {
                    Debug.LogError(
                        $"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has duplicate binding for state '{bindings[index].State}'.",
                        this);
                }
            }

            ValidateRequiredState(configuredStates, PuzzleStartButtonState.Free);
            ValidateRequiredState(configuredStates, PuzzleStartButtonState.Coins);
            ValidateRequiredState(configuredStates, PuzzleStartButtonState.RewardedAd);
            ValidateRequiredState(configuredStates, PuzzleStartButtonState.Busy);
        }

        private void ValidateRequiredState(
            HashSet<PuzzleStartButtonState> configuredStates,
            PuzzleStartButtonState state)
        {
            if (!configuredStates.Contains(state))
            {
                Debug.LogWarning(
                    $"{nameof(PuzzleStartButtonStateBooleanBinder)} on '{name}' has no binding for required state '{state}'.",
                    this);
            }
        }
    }

    [Serializable]
    public sealed class PuzzleStartButtonStateBinding : BooleanEnumStateBinding<PuzzleStartButtonState>
    {
        [SerializeField] private GameObject target;
        [SerializeField] private Graphic background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string labelText;
        [SerializeField] private Color backgroundColor = Color.white;
        [SerializeField] private PuzzleStartButtonState state;

        public override PuzzleStartButtonState State => state;

        public void Apply(PuzzleStartButtonState activeState, PuzzleStartButtonViewModel startButton)
        {
            ApplyActive(State == activeState);

            if (State == activeState)
            {
                RenderContent(startButton ?? PuzzleStartButtonViewModel.None);
            }
        }

        public void Validate(UnityEngine.Object context, string ownerName, int index)
        {
            if (target == null)
            {
                Debug.LogError(
                    $"{nameof(PuzzleStartButtonStateBinding)} #{index} on '{ownerName}' has no target for state '{State}'.",
                    context);
            }

            if (label == null && !string.IsNullOrEmpty(labelText))
            {
                Debug.LogWarning(
                    $"{nameof(PuzzleStartButtonStateBinding)} #{index} on '{ownerName}' has fallback label text but no label reference for state '{State}'.",
                    context);
            }

            if (label != null && string.IsNullOrEmpty(labelText))
            {
                Debug.LogWarning(
                    $"{nameof(PuzzleStartButtonStateBinding)} #{index} on '{ownerName}' has label reference but empty fallback label text for state '{State}'.",
                    context);
            }
        }

        protected override void OnActiveChanged(bool isActive)
        {
            if (target != null)
            {
                target.SetActive(isActive);
            }

            if (isActive && background != null)
            {
                background.color = backgroundColor;
            }
        }

        private void RenderContent(PuzzleStartButtonViewModel startButton)
        {
            if (label == null)
            {
                return;
            }

            label.text = State == startButton.State && !string.IsNullOrEmpty(startButton.LabelText)
                ? startButton.LabelText
                : labelText;
        }
    }
}
