using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleFlow.Presentation.State
{

    [Serializable]
    public abstract class BooleanStateBinding
    {
        protected void ApplyActive(bool isActive)
        {
            OnActiveChanged(isActive);
        }

        protected virtual void OnActiveChanged(bool isActive)
        {
        }
    }

    [Serializable]
    public abstract class BooleanEnumStateBinding<TState> : BooleanStateBinding
        where TState : struct, Enum
    {
        public abstract TState State { get; }

        public void Apply(TState activeState)
        {
            ApplyActive(EqualityComparer<TState>.Default.Equals(State, activeState));
        }
    }

    public abstract class BooleanEnumViewStateBinder<TState> : MonoBehaviour
        where TState : struct, Enum
    {
        protected abstract IReadOnlyList<BooleanEnumStateBinding<TState>> Bindings { get; }

        protected void ApplyBindings(TState state)
        {
            IReadOnlyList<BooleanEnumStateBinding<TState>> bindings = Bindings;
            if (bindings == null)
            {
                return;
            }

            for (int index = 0; index < bindings.Count; index++)
            {
                bindings[index]?.Apply(state);
            }
        }
    }
}
