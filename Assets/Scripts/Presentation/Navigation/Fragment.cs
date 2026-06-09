using UnityEngine;
using UnityEngine.Events;

namespace PuzzleFlow.Presentation.Navigation
{
    public enum FragmentLifecycleState
    {
        None,
        Appearing,
        Appeared,
        Disappearing,
        Disappeared
    }

    public abstract class Fragment : MonoBehaviour
    {
        [SerializeField] private string fragmentId;
        [SerializeField] private Canvas canvas;
        [SerializeField] private bool deactivateOnDisappear = true;

        public UnityEvent OnWillAppearAction;
        public UnityEvent OnAppearedAction;
        public UnityEvent OnWillDisappearAction;
        public UnityEvent OnDisappearedAction;

        public string FragmentId => fragmentId;
        public FragmentLifecycleState LifecycleState { get; private set; } = FragmentLifecycleState.None;
        public bool IsTransitioning =>
            LifecycleState == FragmentLifecycleState.Appearing ||
            LifecycleState == FragmentLifecycleState.Disappearing;

        public void SetSortingOrder(int sortingOrder)
        {
            if (canvas == null)
            {
                return;
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        public void PrepareHidden()
        {
            LifecycleState = FragmentLifecycleState.Disappeared;
            gameObject.SetActive(false);
        }

        public void ShowFragment(object args)
        {
            if (LifecycleState == FragmentLifecycleState.Appearing ||
                LifecycleState == FragmentLifecycleState.Appeared)
            {
                return;
            }

            LifecycleState = FragmentLifecycleState.Appearing;
            gameObject.SetActive(true);
            OnEnabledBeforeAppearing();
            OnWillAppear(args);
            LifecycleState = FragmentLifecycleState.Appeared;
            OnAppeared();
        }

        public void HideFragment()
        {
            if (LifecycleState == FragmentLifecycleState.Disappearing ||
                LifecycleState == FragmentLifecycleState.Disappeared)
            {
                return;
            }

            LifecycleState = FragmentLifecycleState.Disappearing;
            OnWillDisappear();
            LifecycleState = FragmentLifecycleState.Disappeared;
            OnDisappeared();

            if (deactivateOnDisappear)
            {
                gameObject.SetActive(false);
            }
        }

        public void RebuildFragment(object args)
        {
            OnRebuild(args);
        }

        protected virtual void OnEnabledBeforeAppearing() { }
        protected virtual void OnRebuild(object args) { }
        protected virtual void OnWillAppear(object args)
        {
            OnWillAppearAction?.Invoke();
        }

        protected virtual void OnAppeared()
        {
            OnAppearedAction?.Invoke();
        }

        protected virtual void OnWillDisappear()
        {
            OnWillDisappearAction?.Invoke();
        }

        protected virtual void OnDisappeared()
        {
            OnDisappearedAction?.Invoke();
        }
    }
}

