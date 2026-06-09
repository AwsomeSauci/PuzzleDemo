using PuzzleFlow.Presentation.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace PuzzleFlow.Runtime
{
    public sealed class ReusableFragmentFactory : IFragmentFactory, IPreloadableFragmentFactory
    {
        private readonly FragmentRegistry registry;
        private readonly DiContainer container;
        private readonly Dictionary<string, IRoutableFragment> fragmentsById = new Dictionary<string, IRoutableFragment>();

        public ReusableFragmentFactory(
            FragmentRegistry registry,
            DiContainer container)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            this.registry.ValidateOrThrow();
        }

        public FragmentLayer GetLayer(string fragmentId)
        {
            return GetLayer(new FragmentId(fragmentId));
        }

        public FragmentLayer GetLayer(FragmentId fragmentId)
        {
            return GetRegistration(fragmentId).Layer;
        }

        public IRoutableFragment GetOrCreate(string fragmentId, Transform parent, int sortingOrder)
        {
            return GetOrCreate(new FragmentId(fragmentId), parent, sortingOrder);
        }

        public IRoutableFragment GetOrCreate(FragmentId fragmentId, Transform parent, int sortingOrder)
        {
            if (fragmentId.IsEmpty)
            {
                throw new ArgumentOutOfRangeException(nameof(fragmentId), fragmentId, null);
            }

            string fragmentKey = fragmentId.Value;
            if (fragmentsById.TryGetValue(fragmentKey, out IRoutableFragment existing) && IsAlive(existing))
            {
                ApplyRuntimeSettings(existing, parent, sortingOrder);
                return existing;
            }

            FragmentRegistration registration = GetRegistration(fragmentId);
            MonoBehaviour instance = (MonoBehaviour)container.InstantiatePrefabForComponent(
                registration.Prefab.GetType(),
                registration.Prefab,
                parent,
                Array.Empty<object>());
            if (instance is not IRoutableFragment fragment)
            {
                throw new InvalidOperationException($"Registered prefab for '{fragmentId}' does not implement {nameof(IRoutableFragment)}.");
            }

            ApplyRuntimeSettings(fragment, parent, sortingOrder);
            fragmentsById[fragmentKey] = fragment;
            return fragment;
        }

        public void PreloadAll(Transform screenRoot, Transform overlayRoot)
        {
            IReadOnlyList<FragmentRegistration> registrations = registry.Registrations;
            for (int index = 0; index < registrations.Count; index++)
            {
                FragmentRegistration registration = registrations[index];
                if (registration == null || string.IsNullOrEmpty(registration.FragmentId))
                {
                    continue;
                }

                Transform parent = registration.Layer == FragmentLayer.Screen ? screenRoot : overlayRoot;
                int sortingOrder = registration.Layer == FragmentLayer.Screen ? 100 : 200 + index;
                IRoutableFragment fragment = GetOrCreate(registration.Id, parent, sortingOrder);
                if (fragment is Fragment uiFragment)
                {
                    uiFragment.PrepareHidden();
                }
            }
        }

        private static bool IsAlive(IRoutableFragment fragment)
        {
            return fragment is not UnityEngine.Object unityObject || unityObject != null;
        }

        private static void ApplyRuntimeSettings(IRoutableFragment fragment, Transform parent, int sortingOrder)
        {
            if (fragment is not MonoBehaviour behaviour || behaviour == null)
            {
                return;
            }

            behaviour.transform.SetParent(parent, false);
            if (fragment is Fragment uiFragment)
            {
                uiFragment.SetSortingOrder(sortingOrder);
            }
        }

        private FragmentRegistration GetRegistration(FragmentId fragmentId)
        {
            if (registry.TryGet(fragmentId, out FragmentRegistration registration))
            {
                return registration;
            }

            throw new ArgumentOutOfRangeException(nameof(fragmentId), fragmentId, null);
        }
    }
}
