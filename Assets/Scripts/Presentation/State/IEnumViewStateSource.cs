using System;

namespace PuzzleFlow.Presentation.State
{
    public interface IEnumViewStateSource<TState>
        where TState : struct, Enum
    {
        TState CurrentState { get; }
        event Action<TState> StateChanged;
    }
}