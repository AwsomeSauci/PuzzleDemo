using UnityEngine;

namespace PuzzleFlow.Presentation.Navigation
{
    public abstract class FragmentBase<TArgs, TResult> : Fragment, IRoutableFragment
    {
        private IFragmentController controller;

        public void Open(object args, IFragmentController controller)
        {
            this.controller = controller;
            ShowFragment(args);
        }

        public void Rebuild(object args)
        {
            RebuildFragment(args);
        }

        protected override void OnWillAppear(object args)
        {
            base.OnWillAppear(args);
            OnOpen(CastArgs(args));
        }

        protected override void OnWillDisappear()
        {
            base.OnWillDisappear();
            OnClose();
        }

        protected override void OnRebuild(object args)
        {
            OnRefresh(CastArgs(args));
        }

        protected abstract void OnOpen(TArgs args);
        protected virtual void OnRefresh(TArgs args)
        {
            OnOpen(args);
        }

        protected virtual void OnClose() { }

        protected void Close(TResult result)
        {
            controller?.Close(result);
        }

        private static TArgs CastArgs(object args)
        {
            return args == null ? default : (TArgs)args;
        }
    }
}

