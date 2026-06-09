using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using PuzzleFlow.Domain;

namespace PuzzleFlow.Media
{
    public interface IMediaService : IDisposable
    {
        UniTask<TAsset> LoadAsync<TAsset>(
            MediaReference reference,
            CancellationToken cancellationToken = default)
            where TAsset : UnityEngine.Object;

        void Clear();
    }
}