using System;

namespace PuzzleFlow.Media
{
    public readonly struct MediaLoadResult<TAsset>
        where TAsset : UnityEngine.Object
    {
        public MediaLoadResult(TAsset asset, IDisposable lease)
        {
            Asset = asset;
            Lease = lease;
        }

        public TAsset Asset { get; }
        public IDisposable Lease { get; }
    }
}