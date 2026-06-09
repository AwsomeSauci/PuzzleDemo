using PuzzleFlow.Presentation.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Runtime
{
    public sealed class FragmentRegistry : ScriptableObject
    {
        [SerializeField] private FragmentRegistration[] registrations;

        public IReadOnlyList<FragmentRegistration> Registrations => registrations ?? Array.Empty<FragmentRegistration>();

        public bool TryGet(string fragmentId, out FragmentRegistration registration)
        {
            return TryGet(new FragmentId(fragmentId), out registration);
        }

        public bool TryGet(FragmentId fragmentId, out FragmentRegistration registration)
        {
            if (registrations != null)
            {
                for (int index = 0; index < registrations.Length; index++)
                {
                    FragmentRegistration candidate = registrations[index];
                    if (candidate != null && candidate.Id == fragmentId)
                    {
                        registration = candidate;
                        return true;
                    }
                }
            }

            registration = null;
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
                $"{nameof(FragmentRegistry)} '{name}' is invalid:{Environment.NewLine}" +
                string.Join(Environment.NewLine, errors));
        }

        private void CollectValidationErrors(List<string> errors)
        {
            if (registrations == null || registrations.Length == 0)
            {
                errors.Add("Fragment registry has no registrations.");
                return;
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < registrations.Length; index++)
            {
                FragmentRegistration registration = registrations[index];
                if (registration == null)
                {
                    errors.Add($"Registration #{index} is null.");
                    continue;
                }

                registration.CollectValidationErrors(index, errors);
                string id = registration.Id.Value;
                if (!string.IsNullOrEmpty(id) && !ids.Add(id))
                {
                    errors.Add($"Registration #{index} duplicates fragment id '{id}'.");
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            IReadOnlyList<string> errors = GetValidationErrors();
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError($"{nameof(FragmentRegistry)} '{name}': {errors[index]}", this);
            }
        }
#endif
    }

    [Serializable]
    public sealed class FragmentRegistration
    {
        [SerializeField] private string fragmentId;
        [SerializeField] private FragmentLayer layer;
        [SerializeField] private MonoBehaviour prefab;

        public string FragmentId => fragmentId;
        public PuzzleFlow.Presentation.Navigation.FragmentId Id =>
            new PuzzleFlow.Presentation.Navigation.FragmentId(fragmentId);
        public FragmentLayer Layer => layer;
        public MonoBehaviour Prefab => prefab;

        public void CollectValidationErrors(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(fragmentId))
            {
                errors.Add($"Registration #{index} has empty fragment id.");
            }

            if (!Enum.IsDefined(typeof(FragmentLayer), layer))
            {
                errors.Add($"Registration #{index} has invalid layer '{layer}'.");
            }

            if (prefab == null)
            {
                errors.Add($"Registration #{index} for '{Id}' has no prefab.");
                return;
            }

            if (prefab is not IRoutableFragment)
            {
                errors.Add(
                    $"Registration #{index} for '{Id}' references '{prefab.name}', " +
                    $"but it does not implement {nameof(IRoutableFragment)}.");
            }
        }
    }
}


